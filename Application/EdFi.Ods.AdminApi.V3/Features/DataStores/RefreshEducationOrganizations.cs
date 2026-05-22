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
                "Refreshes education organizations for all data stores",
                "Triggers a refresh of education organization data from all data stores"
            )
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dataStores/{dataStoreId}/edOrgs/refresh", RefreshEducationOrganizationsByDataStore)
            .WithSummaryAndDescription(
                "Refreshes education organizations for a specific data store",
                "Triggers a refresh of education organization data for the specified data store"
            )
            .WithRouteOptions(b => b
                .WithResponseCode(202)
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

        return Results.Accepted(null, new
        {
            Message = "Education organizations refresh has been queued for all data stores"
        });
    }

    public static async Task<IResult> RefreshEducationOrganizationsByDataStore(
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IGetDataStoreQuery getDataStoreQuery,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        int dataStoreId)
    {
        var dataStore = getDataStoreQuery.Execute(dataStoreId);
        if (dataStore == null)
        {
            throw new NotFoundException<int>("DataStore", dataStoreId);
        }

        var tenantConfiguration = tenantConfigurationProvider.Get();
        var tenantIdentifier = tenantConfiguration?.TenantIdentifier;

        var job = JobBuilder.Create<RefreshEducationOrganizationsJob>()
            .WithIdentity($"{JobConstants.RefreshEducationOrganizationsJobName}-{tenantIdentifier}-{Guid.NewGuid()}")
            .UsingJobData(JobConstants.TenantNameKey, tenantIdentifier)
            .UsingJobData(JobConstants.OdsInstanceIdKey, dataStoreId)
            .Build();

        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.ScheduleJob(job, trigger);

        return Results.Accepted(null, new
        {
            Message = $"Education organizations refresh has been queued for data store {dataStoreId}"
        });
    }
}
