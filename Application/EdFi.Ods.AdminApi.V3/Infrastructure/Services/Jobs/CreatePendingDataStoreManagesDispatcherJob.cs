// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class CreatePendingDataStoreManagesDispatcherJob(
    ILogger<CreatePendingDataStoreManagesDispatcherJob> logger,
    IJobStatusService jobStatusService,
    AdminApiDbContext dbContext,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    IOptions<AppSettings> options)
    : AdminApiQuartzJobBase(logger, jobStatusService)
{
    private const int DefaultMaxRetryAttempts = 3;

    private readonly AdminApiDbContext _dbContext = dbContext;
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly IOptions<AppSettings> _options = options;

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var tenantName = GetTenantName(context, multiTenancyEnabled);
        AdminApiDbContext? tenantAdminApiDbContext = null;
        var adminApiDbContext = _dbContext;

        try
        {
            if (multiTenancyEnabled)
            {
                tenantAdminApiDbContext = _tenantSpecificDbContextProvider.GetAdminApiDbContext(tenantName!);
                adminApiDbContext = tenantAdminApiDbContext;
            }

            var eligibleOdsInstanceManages = await adminApiDbContext.OdsInstanceManages
                .Where(instance => instance.Status == OdsInstanceManageStatus.PendingCreate.ToString() || instance.Status == OdsInstanceManageStatus.CreateFailed.ToString())
                .OrderBy(instance => instance.Id)
                .ToListAsync();

            foreach (var odsInstanceManage in eligibleOdsInstanceManages)
            {
                if (string.Equals(odsInstanceManage.Status, OdsInstanceManageStatus.PendingCreate.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    await ScheduleCreateJobAsync(context, odsInstanceManage.Id, tenantName);
                    continue;
                }

                if (!await IsRetryEligibleAsync(adminApiDbContext, odsInstanceManage, tenantName))
                {
                    odsInstanceManage.Status = OdsInstanceManageStatus.CreateError.ToString();
                    odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                    odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                    await adminApiDbContext.SaveChangesAsync();
                    continue;
                }

                odsInstanceManage.Status = OdsInstanceManageStatus.PendingCreate.ToString();
                odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();

                await ScheduleCreateJobAsync(context, odsInstanceManage.Id, tenantName);
            }
        }
        finally
        {
            if (tenantAdminApiDbContext is not null)
            {
                await tenantAdminApiDbContext.DisposeAsync();
            }
        }
    }

    private async Task<bool> IsRetryEligibleAsync(AdminApiDbContext adminApiDbContext, OdsInstanceManage odsInstanceManage, string? tenantName)
    {
        var maxRetryAttempts = _options.Value.CreateOdsInstanceManagesMaxRetryAttempts > 0
            ? _options.Value.CreateOdsInstanceManagesMaxRetryAttempts
            : DefaultMaxRetryAttempts;

        var jobIdPrefix = $"{CreateInstanceJob.BuildJobIdentity(odsInstanceManage.Id, tenantName)}_";
        var errorCount = await adminApiDbContext.JobStatuses
            .CountAsync(status => status.JobId.StartsWith(jobIdPrefix) && status.Status == QuartzJobStatus.Error.ToString());

        return errorCount < maxRetryAttempts;
    }

    private static async Task ScheduleCreateJobAsync(IJobExecutionContext context, int odsInstanceManageId, string? tenantName)
    {
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.OdsInstanceManageIdKey] = odsInstanceManageId
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        await QuartzJobScheduler.ScheduleJob<CreateInstanceJob>(
            context.Scheduler,
            CreateInstanceJob.CreateJobKey(odsInstanceManageId, tenantName),
            jobData,
            startImmediately: true);
    }

    private static string? GetTenantName(IJobExecutionContext context, bool multiTenancyEnabled)
    {
        if (!multiTenancyEnabled)
        {
            return null;
        }

        var tenantName = context.MergedJobDataMap.ContainsKey(JobConstants.TenantNameKey)
            ? context.MergedJobDataMap.GetString(JobConstants.TenantNameKey)
            : null;

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new InvalidOperationException(
                $"{JobConstants.TenantNameKey} must be provided when multi-tenancy is enabled.");
        }

        return tenantName;
    }
}
