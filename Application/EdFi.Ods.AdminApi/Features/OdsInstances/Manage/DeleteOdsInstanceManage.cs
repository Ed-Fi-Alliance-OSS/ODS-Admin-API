// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public class DeleteOdsInstanceManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapDelete(endpoints, "/odsInstances/manage/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(
        IGetOdsInstanceManageByIdQuery getOdsInstanceManageByIdQuery,
        IDeleteOdsInstanceManageCommand deleteOdsInstanceManageCommand,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        int id
    )
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var odsInstanceManage = getOdsInstanceManageByIdQuery.Execute(id);
        if (odsInstanceManage is null)
            throw new NotFoundException<int>("odsInstanceManage", id);

        if (odsInstanceManage.Status == OdsInstanceManageStatus.Deleted.ToString())
            throw new NotFoundException<int>("odsInstanceManage", id);

        var blockingMessage = GetBlockingStatusMessage(odsInstanceManage.Status);
        if (blockingMessage is not null)
            throw new ValidationException([new ValidationFailure(nameof(id), blockingMessage)]);

        deleteOdsInstanceManageCommand.Execute(id);

        var tenantName = options.Value.MultiTenancy
            ? tenantConfigurationProvider.Get()?.TenantIdentifier
            : null;
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.OdsInstanceManageIdKey] = id
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
            // The DeletePendingOdsInstanceManagesDispatcherJob may have already scheduled this job.
            // Treat duplicate scheduling as success — the job is already queued.
        }

        return Results.NoContent();
    }

    private static string? GetBlockingStatusMessage(string status)
    {
        if (Enum.TryParse<OdsInstanceManageStatus>(status, ignoreCase: true, out var parsed))
        {
            return parsed switch
            {
                OdsInstanceManageStatus.PendingCreate    => "OdsInstanceManage is being provisioned. Wait for creation to complete.",
                OdsInstanceManageStatus.CreateInProgress => "OdsInstanceManage is currently being provisioned. Wait for creation to complete.",
                OdsInstanceManageStatus.CreateFailed     => "OdsInstanceManage creation failed. It will be retried automatically by the background job.",
                OdsInstanceManageStatus.CreateError      => "OdsInstanceManage creation failed permanently. Manual database intervention required before deleting.",
                OdsInstanceManageStatus.PendingDelete    => "OdsInstanceManage is already queued for deletion.",
                OdsInstanceManageStatus.DeleteInProgress => "OdsInstanceManage is currently being deleted.",
                OdsInstanceManageStatus.DeleteFailed     => "OdsInstanceManage deletion failed. It will be retried automatically by the background job.",
                OdsInstanceManageStatus.DeleteError      => "OdsInstanceManage deletion failed permanently. Manual database intervention required.",
                _ => null,
            };
        }

        return null;
    }
}
