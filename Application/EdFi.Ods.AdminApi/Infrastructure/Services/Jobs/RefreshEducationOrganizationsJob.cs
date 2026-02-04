// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;

public class RefreshEducationOrganizationsJob(
    ILogger<RefreshEducationOrganizationsJob> logger,
    IJobStatusService jobStatusService,
    IEducationOrganizationService edOrgService,
    IOptions<AppSettings> options) : AdminApiQuartzJobBase(logger, jobStatusService)
{
    private readonly IEducationOrganizationService _edOrgService = edOrgService;

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        var multiTenancyEnabled = options.Value.MultiTenancy;
        var jobId = context.JobDetail.Key.Name;
        var instanceId = context.MergedJobDataMap.ContainsKey(JobConstants.OdsInstanceIdKey)
            ? context.MergedJobDataMap.GetInt(JobConstants.OdsInstanceIdKey)
            : (int?)null;
        if (multiTenancyEnabled && context.MergedJobDataMap.ContainsKey(JobConstants.TenantNameKey))
        {
            var tenantName = context.MergedJobDataMap.GetString(JobConstants.TenantNameKey);

            if (!string.IsNullOrEmpty(tenantName))
            {
                logger.LogInformation(
                    "Starting RefreshEducationOrganizationsJob for Tenant: {TenantName}, JobId: {JobId}",
                    tenantName,
                    jobId
                );
                await _edOrgService.Execute(tenantName, instanceId);
            }
            else
            {
                logger.LogError(
                   "Tenant name should be provided in the job data map for multi-tenant execution of RefreshEducationOrganizationsJob. JobId: {JobId}", jobId);
            }
        }
        else
        {
            logger.LogInformation(
                "Starting scheduled RefreshEducationOrganizationsJob with JobId: {JobId}",
                jobId
            );
            await _edOrgService.Execute(null, instanceId);
        }
    }
}