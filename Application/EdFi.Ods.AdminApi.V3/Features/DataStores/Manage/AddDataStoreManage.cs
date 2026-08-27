// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Swashbuckle.AspNetCore.Annotations;
using EdFi.Ods.AdminApi.V3.Infrastructure;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public class AddDataStoreManage : IFeature
{
    // 63 (DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength) minus 17 fixed
    // overhead chars ("EdFi_Ods_" prefix + "_" separator + longest DatabaseTemplate value
    // "Minimal") — the largest Name that can never push the generated DatabaseName over the
    // portable limit, for either DatabaseTemplate value, with no prefix stripping applied.
    private const int MaxDataStoreManageNameLength = 46;
    private static readonly Regex _validDataStoreManageNamePattern = new(
        "^[A-Za-z0-9 _]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dataStores/manage", Handle)
            .WithSummaryAndDescription("Asynchronously creates a data store based on the supplied values", "Asynchronously creates a data store based on the supplied values. The request is accepted and the creation process is queued for processing.")
            .WithRouteOptions(b => b.WithResponseCode(202, "Accepted. The dataStore record has been created and provisioning has been queued; the database is not yet available. The response has no body. Poll the resource identified by the Location header and read its status property, which progresses PendingCreate, CreateInProgress, then Created or CreateFailed.", "Absolute URL of the created dataStore, of the form {scheme}://{host}/v3/dataStores/manage/{id}."))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public async static Task<IResult> Handle(
        Validator validator,
        AddDataStoreManageCommand addDataStoreManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddDataStoreManageRequest request,
        HttpContext httpContext)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure("dataStore", "This endpoint has been disabled on application settings.")]);

        await validator.GuardAsync(request);

        var added = addDataStoreManageCommand.Execute(request);

        var tenantIdentifier = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;

        var jobBuilder = JobBuilder.Create<CreateInstanceJob>()
            .WithIdentity(CreateInstanceJob.CreateJobKey(added.Id, tenantIdentifier))
            .UsingJobData(JobConstants.OdsInstanceManageIdKey, added.Id);

        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            jobBuilder = jobBuilder.UsingJobData(JobConstants.TenantNameKey, tenantIdentifier);
        }

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();

        try
        {
            await scheduler.ScheduleJob(jobBuilder.Build(), trigger);
        }
        catch (ObjectAlreadyExistsException)
        {
            // The CreatePendingDataStoreManagesDispatcherJob may have already scheduled this job
            // (e.g. it fired between the DB insert and this ScheduleJob call). Treat duplicate
            // scheduling as success — the job is already queued and will process the OdsInstanceManage.
        }

        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStores/manage/{added.Id}");
        return Results.Accepted(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDataStoreManageRequest")]
    public class AddDataStoreManageRequest : IAddDataStoreManageModel
    {
        [SwaggerSchema(Description = "Name of the DataStore database (46 characters or fewer)", Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = "Database template to use for the DataStore database", Nullable = false)]
        public string? DatabaseTemplate { get; set; }
    }

    public class Validator : AbstractValidator<AddDataStoreManageRequest>
    {
        private static readonly string[] _validDatabaseTemplates = Enum.GetNames<SandboxType>();
        private readonly AdminApiDbContext _adminApiDbContext;
        private readonly IUsersContext _usersContext;

        public Validator(AdminApiDbContext adminApiDbContext, IUsersContext usersContext)
        {
            _adminApiDbContext = adminApiDbContext;
            _usersContext = usersContext;

            RuleFor(m => m.Name)
                .NotEmpty()
                .MaximumLength(MaxDataStoreManageNameLength)
                .WithMessage($"'{{PropertyName}}' must be {MaxDataStoreManageNameLength} characters or fewer so the generated database name fits within the {DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength}-character portable limit.")
                .Matches(_validDataStoreManageNamePattern)
                .WithMessage("'{PropertyName}' may only contain letters, numbers, spaces, and underscores.");

            RuleFor(m => m.DatabaseTemplate).NotEmpty().MaximumLength(100)
                .Must(t => t != null && _validDatabaseTemplates.Contains(t))
                .WithMessage($"'{{PropertyValue}}' is not a valid database template. Allowed values are: {string.Join(", ", _validDatabaseTemplates)}.");

            RuleFor(m => m).CustomAsync(async (request, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name)
                    || string.IsNullOrWhiteSpace(request.DatabaseTemplate)
                    || request.Name.Length > MaxDataStoreManageNameLength
                    || !_validDataStoreManageNamePattern.IsMatch(request.Name)
                    || !_validDatabaseTemplates.Contains(request.DatabaseTemplate))
                {
                    return;
                }

                var normalizedName = request.Name.Trim();

                if (await _adminApiDbContext.OdsInstanceManages.AnyAsync(instance => instance.Name == normalizedName && instance.Status != OdsInstanceManageStatus.Deleted.ToString(), cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddDataStoreManageRequest.Name),
                        $"A DataStoreManage named '{normalizedName}' already exists.");
                    return;
                }

                if (await _usersContext.OdsInstances.AnyAsync(instance => instance.Name == normalizedName, cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddDataStoreManageRequest.Name),
                        $"A DataStore named '{normalizedName}' already exists.");
                    return;
                }

                var databaseName = DataStoreManageDatabaseNameFormatter.Build(request.Name, request.DatabaseTemplate);

                if (databaseName.Length > DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength)
                {
                    context.AddFailure(
                        nameof(AddDataStoreManageRequest.Name),
                        $"The generated database name '{databaseName}' exceeds the portable limit of {DataStoreManageDatabaseNameFormatter.MaxPortableDatabaseNameLength} characters. Shorten Name or DatabaseTemplate.");
                }
            });
        }
    }
}
