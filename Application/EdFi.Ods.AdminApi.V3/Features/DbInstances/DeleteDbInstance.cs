// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.V3.Features.DbInstances;

public class DeleteDbInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapDelete(endpoints, "/dbInstances/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(
        IGetDbInstanceByIdQuery getDbInstanceByIdQuery,
        IDeleteDbInstanceCommand deleteDbInstanceCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        int id
    )
    {
        var dbInstance = getDbInstanceByIdQuery.Execute(id);
        if (dbInstance is null)
            throw new NotFoundException<int>("dbInstance", id);

        if (dbInstance.Status == DbInstanceStatus.Deleted.ToString())
            throw new NotFoundException<int>("dbInstance", id);

        var blockingMessage = GetBlockingStatusMessage(dbInstance.Status);
        if (blockingMessage is not null)
            throw new ValidationException([new ValidationFailure(nameof(id), blockingMessage)]);

        deleteDbInstanceCommand.Execute(id);

        var tenantName = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.DbInstanceIdKey] = id
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        var scheduler = await schedulerFactory.GetScheduler();

        try
        {
            await QuartzJobScheduler.ScheduleJob<DeleteInstanceJob>(
                scheduler,
                DeleteInstanceJob.CreateJobKey(id, tenantName),
                jobData,
                startImmediately: true);
        }
        catch (ObjectAlreadyExistsException)
        {
            // The DeletePendingDbInstancesDispatcherJob may have already scheduled this job.
            // Treat duplicate scheduling as success — the job is already queued.
        }

        return Results.Accepted($"/dbinstances/{id}", null);
    }

    private static string? GetBlockingStatusMessage(string status)
    {
        if (Enum.TryParse<DbInstanceStatus>(status, out var parsed))
        {
            return parsed switch
            {
                DbInstanceStatus.PendingCreate    => "DbInstance is being provisioned. Wait for creation to complete.",
                DbInstanceStatus.CreateInProgress => "DbInstance is currently being provisioned. Wait for creation to complete.",
                DbInstanceStatus.CreateFailed     => "DbInstance creation failed. It will be retried automatically by the background job.",
                DbInstanceStatus.CreateError      => "DbInstance creation failed permanently. Manual database intervention required before deleting.",
                DbInstanceStatus.PendingDelete    => "DbInstance is already queued for deletion.",
                DbInstanceStatus.DeleteInProgress => "DbInstance is currently being deleted.",
                DbInstanceStatus.DeleteFailed     => "DbInstance deletion failed. It will be retried automatically by the background job.",
                DbInstanceStatus.DeleteError      => "DbInstance deletion failed permanently. Manual database intervention required.",
                _ => null,
            };
        }

        return null;
    }
}
