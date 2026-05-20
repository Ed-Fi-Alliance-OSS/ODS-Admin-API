// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class RefreshEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dataStores/edOrgs/refresh", RefreshAllEducationOrganizations)
            .WithSummaryAndDescription(
                "Refreshes education organizations for all ODS instances",
                "Triggers a refresh of education organization data from all ODS instances"
            )
            .WithRouteOptions(b => b.WithResponseCode(201))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dataStores/{instanceId}/edOrgs/refresh", RefreshEducationOrganizationsByInstance)
            .WithSummaryAndDescription(
                "Refreshes education organizations for a specific ODS instance",
                "Triggers a refresh of education organization data for the specified ODS instance"
            )
            .WithRouteOptions(b => b
                .WithResponseCode(201)
                .WithResponseCode(404))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> RefreshAllEducationOrganizations(
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider)
    {
        var tenantConfiguration = tenantConfigurationProvider.Get();
        var tenantIdentifier = tenantConfiguration?.TenantIdentifier;

        var job = JobBuilder.Create<RefreshEducationOrganizationsJob>()
            .WithIdentity($"{JobConstants.RefreshEducationOrganizationsJobName}-{tenantIdentifier}-{Guid.NewGuid()}")
            .UsingJobData(JobConstants.TenantNameKey, tenantIdentifier)
            .Build();

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ScheduleJob(job, trigger);

        var jobId = $"{job.Key.Name}_{scheduler.SchedulerInstanceId}";
        var response = new
        {
            jobId,
            status = "Pending",
            createdAt = DateTime.UtcNow
        };
        var locationUri = $"/v3/jobs/{jobId}";

        return Results.Created(locationUri, response);
    }

    public static async Task<IResult> RefreshEducationOrganizationsByInstance(
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IGetDataStoreQuery getDataStoreQuery,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        int instanceId)
    {
        var odsInstance = getDataStoreQuery.Execute(instanceId);
        if (odsInstance == null)
        {
            throw new NotFoundException<int>("DataStore", instanceId);
        }

        var tenantConfiguration = tenantConfigurationProvider.Get();
        var tenantIdentifier = tenantConfiguration?.TenantIdentifier;

        var job = JobBuilder.Create<RefreshEducationOrganizationsJob>()
            .WithIdentity($"{JobConstants.RefreshEducationOrganizationsJobName}-{tenantIdentifier}-{Guid.NewGuid()}")
            .UsingJobData(JobConstants.TenantNameKey, tenantIdentifier)
            .UsingJobData(JobConstants.OdsInstanceIdKey, instanceId)
            .Build();

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ScheduleJob(job, trigger);

        var jobId = $"{job.Key.Name}_{scheduler.SchedulerInstanceId}";
        var response = new
        {
            jobId,
            status = "Pending",
            createdAt = DateTime.UtcNow
        };
        var locationUri = $"/v3/jobs/{jobId}";

        return Results.Created(locationUri, response);
    }
}
