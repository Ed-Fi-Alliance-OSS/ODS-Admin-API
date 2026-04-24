// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class DeletePendingDbInstancesDispatcherJob(
    ILogger<DeletePendingDbInstancesDispatcherJob> logger,
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

            var eligibleDbInstances = await adminApiDbContext.DbInstances
                .Where(instance => instance.Status == DbInstanceStatus.PendingDelete.ToString()
                                || instance.Status == DbInstanceStatus.DeleteFailed.ToString())
                .OrderBy(instance => instance.Id)
                .ToListAsync();

            foreach (var dbInstance in eligibleDbInstances)
            {
                if (string.Equals(dbInstance.Status, DbInstanceStatus.PendingDelete.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    await ScheduleDeleteJobAsync(context, dbInstance.Id, tenantName);
                    continue;
                }

                if (!await IsRetryEligibleAsync(adminApiDbContext, dbInstance, tenantName))
                {
                    dbInstance.Status = DbInstanceStatus.DeleteError.ToString();
                    dbInstance.LastModifiedDate = DateTime.UtcNow;
                    dbInstance.LastRefreshed = DateTime.UtcNow;
                    await adminApiDbContext.SaveChangesAsync();
                    continue;
                }

                dbInstance.Status = DbInstanceStatus.PendingDelete.ToString();
                dbInstance.LastModifiedDate = DateTime.UtcNow;
                dbInstance.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();

                await ScheduleDeleteJobAsync(context, dbInstance.Id, tenantName);
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

    private async Task<bool> IsRetryEligibleAsync(AdminApiDbContext adminApiDbContext, DbInstance dbInstance, string? tenantName)
    {
        var maxRetryAttempts = _options.Value.DeleteDbInstancesMaxRetryAttempts > 0
            ? _options.Value.DeleteDbInstancesMaxRetryAttempts
            : DefaultMaxRetryAttempts;

        var jobIdPrefix = $"{DeleteInstanceJob.BuildJobIdentity(dbInstance.Id, tenantName)}_";
        var errorCount = await adminApiDbContext.JobStatuses
            .CountAsync(status => status.JobId.StartsWith(jobIdPrefix) && status.Status == QuartzJobStatus.Error.ToString());

        return errorCount < maxRetryAttempts;
    }

    private static async Task ScheduleDeleteJobAsync(IJobExecutionContext context, int dbInstanceId, string? tenantName)
    {
        var jobData = new Dictionary<string, object>
        {
            [JobConstants.DbInstanceIdKey] = dbInstanceId
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobData[JobConstants.TenantNameKey] = tenantName;
        }

        await QuartzJobScheduler.ScheduleJob<DeleteInstanceJob>(
            context.Scheduler,
            DeleteInstanceJob.CreateJobKey(dbInstanceId, tenantName),
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
