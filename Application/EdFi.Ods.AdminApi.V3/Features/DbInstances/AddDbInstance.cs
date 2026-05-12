// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

using EdFi.Admin.DataAccess.Contexts;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Swashbuckle.AspNetCore.Annotations;
using EdFi.Ods.AdminApi.V3.Infrastructure;

namespace EdFi.Ods.AdminApi.V3.Features.DbInstances;

public class AddDbInstance : IFeature
{
    private const int MaxSynchronizedNameLength = 100;
    private const int MaxDbInstanceNameLength = MaxSynchronizedNameLength;
    private static readonly Regex _validDbInstanceNamePattern = new(
        "^[A-Za-z0-9 _]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dbInstances", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public async static Task<IResult> Handle(
        Validator validator,
        AddDbInstanceCommand addDbInstanceCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        AddDbInstanceRequest request,
        HttpContext httpContext)
    {
        await validator.GuardAsync(request);

        var added = addDbInstanceCommand.Execute(request);

        var tenantIdentifier = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;

        var jobBuilder = JobBuilder.Create<CreateInstanceJob>()
            .WithIdentity(CreateInstanceJob.CreateJobKey(added.Id, tenantIdentifier))
            .UsingJobData(JobConstants.DbInstanceIdKey, added.Id);

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
            // The CreatePendingDbInstancesDispatcherJob may have already scheduled this job
            // (e.g. it fired between the DB insert and this ScheduleJob call). Treat duplicate
            // scheduling as success — the job is already queued and will process the DbInstance.
        }

        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dbinstances/{added.Id}");
        return Results.Accepted(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDbInstanceRequest")]
    public class AddDbInstanceRequest : IAddDbInstanceModel
    {
        [SwaggerSchema(Description = "Name of the database instance", Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = "Database template to use for the instance", Nullable = false)]
        public string? DatabaseTemplate { get; set; }
    }

    public class Validator : AbstractValidator<AddDbInstanceRequest>
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
                .MaximumLength(MaxDbInstanceNameLength)
                .WithMessage($"'{{PropertyName}}' must be {MaxDbInstanceNameLength} characters or fewer so the synchronized ODS instance name fits within {MaxSynchronizedNameLength} characters.")
                .Matches(_validDbInstanceNamePattern)
                .WithMessage("'{PropertyName}' may only contain letters, numbers, spaces, and underscores.");

            RuleFor(m => m.DatabaseTemplate).NotEmpty().MaximumLength(100)
                .Must(t => t != null && _validDatabaseTemplates.Contains(t))
                .WithMessage($"'{{PropertyValue}}' is not a valid database template. Allowed values are: {string.Join(", ", _validDatabaseTemplates)}.");

            RuleFor(m => m).CustomAsync(async (request, context, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name)
                    || string.IsNullOrWhiteSpace(request.DatabaseTemplate)
                    || request.Name.Length > MaxDbInstanceNameLength
                    || !_validDbInstanceNamePattern.IsMatch(request.Name)
                    || !_validDatabaseTemplates.Contains(request.DatabaseTemplate))
                {
                    return;
                }

                var normalizedName = request.Name.Trim();

                if (await _adminApiDbContext.DbInstances.AnyAsync(instance => instance.Name == normalizedName && instance.Status != DbInstanceStatus.Deleted.ToString(), cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddDbInstanceRequest.Name),
                        $"A DbInstance named '{normalizedName}' already exists.");
                    return;
                }

                if (await _usersContext.OdsInstances.AnyAsync(instance => instance.Name == normalizedName, cancellationToken))
                {
                    context.AddFailure(
                        nameof(AddDbInstanceRequest.Name),
                        $"An OdsInstance named '{normalizedName}' already exists.");
                    return;
                }

                var databaseName = DbInstanceDatabaseNameFormatter.Build(request.Name, request.DatabaseTemplate);

                if (databaseName.Length > DbInstanceDatabaseNameFormatter.MaxPortableDatabaseNameLength)
                {
                    context.AddFailure(
                        nameof(AddDbInstanceRequest.Name),
                        $"The generated database name '{databaseName}' exceeds the portable limit of {DbInstanceDatabaseNameFormatter.MaxPortableDatabaseNameLength} characters. Shorten Name or DatabaseTemplate.");
                }
            });
        }
    }
}
