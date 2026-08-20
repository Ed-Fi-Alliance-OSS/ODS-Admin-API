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
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public class AddOdsInstanceManage : IFeature
{
    // 63 (OdsInstanceManageDatabaseNameFormatter.MaxPortableDatabaseNameLength) minus 17 fixed
    // overhead chars ("EdFi_Ods_" prefix + "_" separator + longest DatabaseTemplate value
    // "Minimal") — the largest Name that can never push the generated DatabaseName over the
    // portable limit, for either DatabaseTemplate value, with no prefix stripping applied.
    private const int MaxOdsInstanceManageNameLength = 46;
    private static readonly Regex _validOdsInstanceManageNamePattern = new(
        "^[A-Za-z0-9 _]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/odsInstances/manage", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public async static Task<IResult> Handle(
        Validator validator,
        AddOdsInstanceManageCommand addOdsInstanceManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddOdsInstanceManageRequest request)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        await validator.GuardAsync(request);

        var added = addOdsInstanceManageCommand.Execute(request);

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
            // The CreatePendingOdsInstanceManagesDispatcherJob may have already scheduled this job
            // (e.g. it fired between the DB insert and this ScheduleJob call). Treat duplicate
            // scheduling as success — the job is already queued and will process the OdsInstanceManage.
        }

        return Results.Accepted($"/odsinstances/manage/{added.Id}", null);
    }

    [SwaggerSchema(Title = "AddOdsInstanceManageRequest")]
    public class AddOdsInstanceManageRequest : IAddOdsInstanceManageModel
    {
        [SwaggerSchema(Description = "Name of the database instance (46 characters or fewer)", Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = "Database template to use for the instance", Nullable = false)]
        public string? DatabaseTemplate { get; set; }
    }

    public class Validator : AbstractValidator<AddOdsInstanceManageRequest>
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
                .MaximumLength(MaxOdsInstanceManageNameLength)
                .WithMessage($"'{{PropertyName}}' must be {MaxOdsInstanceManageNameLength} characters or fewer so the generated database name fits within the {OdsInstanceManageDatabaseNameFormatter.MaxPortableDatabaseNameLength}-character portable limit.")
                .Matches(_validOdsInstanceManageNamePattern)
                .WithMessage("'{PropertyName}' may only contain letters, numbers, spaces, and underscores.");

            RuleFor(m => m.DatabaseTemplate).NotEmpty().MaximumLength(100)
                .Must(t => t != null && _validDatabaseTemplates.Contains(t))
                .WithMessage($"'{{PropertyValue}}' is not a valid database template. Allowed values are: {string.Join(", ", _validDatabaseTemplates)}.");

            RuleFor(m => m).CustomAsync(async (request, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name)
                    || string.IsNullOrWhiteSpace(request.DatabaseTemplate)
                    || request.Name.Length > MaxOdsInstanceManageNameLength
                    || !_validOdsInstanceManageNamePattern.IsMatch(request.Name)
                    || !_validDatabaseTemplates.Contains(request.DatabaseTemplate))
                {
                    return;
                }

                var normalizedName = request.Name.Trim();

                if (await _adminApiDbContext.OdsInstanceManages.AnyAsync(instance => instance.Name == normalizedName && instance.Status != OdsInstanceManageStatus.Deleted.ToString(), cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddOdsInstanceManageRequest.Name),
                        $"An OdsInstanceManage named '{normalizedName}' already exists.");
                    return;
                }

                if (await _usersContext.OdsInstances.AnyAsync(instance => instance.Name == normalizedName, cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddOdsInstanceManageRequest.Name),
                        $"An OdsInstance named '{normalizedName}' already exists.");
                    return;
                }

                var databaseName = OdsInstanceManageDatabaseNameFormatter.Build(request.Name, request.DatabaseTemplate);

                if (databaseName.Length > OdsInstanceManageDatabaseNameFormatter.MaxPortableDatabaseNameLength)
                {
                    context.AddFailure(
                        nameof(AddOdsInstanceManageRequest.Name),
                        $"The generated database name '{databaseName}' exceeds the portable limit of {OdsInstanceManageDatabaseNameFormatter.MaxPortableDatabaseNameLength} characters. Shorten Name or DatabaseTemplate.");
                }
            });
        }
    }
}
