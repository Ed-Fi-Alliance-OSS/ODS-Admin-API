// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class DeleteInstanceJob(
    ILogger<DeleteInstanceJob> logger,
    IJobStatusService jobStatusService,
    AdminApiDbContext dbContext,
    IUsersContext usersContext,
    ITenantConfigurationProvider tenantConfigurationProvider,
    IContextProvider<TenantConfiguration> tenantConfigurationContextProvider,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    ISandboxProvisioner sandboxProvisioner,
    IOptions<AppSettings> options)
    : AdminApiQuartzJobBase(logger, jobStatusService)
{
    private readonly AdminApiDbContext _dbContext = dbContext;
    private readonly IUsersContext _usersContext = usersContext;
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider = tenantConfigurationProvider;
    private readonly IContextProvider<TenantConfiguration> _tenantConfigurationContextProvider = tenantConfigurationContextProvider;
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly ISandboxProvisioner _sandboxProvisioner = sandboxProvisioner;
    private readonly IOptions<AppSettings> _options = options;

    internal static JobKey CreateJobKey(int dbInstanceId, string? tenantName)
        => new(BuildJobIdentity(dbInstanceId, tenantName));

    internal static string BuildJobIdentity(int dbInstanceId, string? tenantName)
        => string.IsNullOrWhiteSpace(tenantName)
            ? $"{JobConstants.DeleteInstanceJobName}-{dbInstanceId}"
            : $"{JobConstants.DeleteInstanceJobName}-{tenantName}-{dbInstanceId}";

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        if (!context.MergedJobDataMap.ContainsKey(JobConstants.DbInstanceIdKey))
        {
            throw new InvalidOperationException($"{JobConstants.DbInstanceIdKey} must be provided for {JobConstants.DeleteInstanceJobName}.");
        }

        var dbInstanceId = context.MergedJobDataMap.GetInt(JobConstants.DbInstanceIdKey);
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var tenantName = GetTenantName(context, multiTenancyEnabled);

        AdminApiDbContext? tenantAdminApiDbContext = null;
        IUsersContext? tenantUsersContext = null;
        var adminApiDbContext = _dbContext;
        var resolvedUsersContext = _usersContext;
        DbInstance? dbInstance = null;

        try
        {
            if (multiTenancyEnabled)
            {
                if (!_tenantConfigurationProvider.Get().TryGetValue(tenantName!, out var tenantConfiguration)
                    || tenantConfiguration is null)
                {
                    throw new InvalidOperationException($"Tenant '{tenantName}' is not configured.");
                }

                _tenantConfigurationContextProvider.Set(tenantConfiguration);
                tenantAdminApiDbContext = _tenantSpecificDbContextProvider.GetAdminApiDbContext(tenantName!);
                tenantUsersContext = _tenantSpecificDbContextProvider.GetUsersContext(tenantName!);
                adminApiDbContext = tenantAdminApiDbContext;
                resolvedUsersContext = tenantUsersContext;
            }

            dbInstance = await adminApiDbContext.DbInstances
                .FirstOrDefaultAsync(instance => instance.Id == dbInstanceId);

            if (dbInstance is null)
            {
                throw new InvalidOperationException($"DbInstance '{dbInstanceId}' was not found.");
            }

            // Guard against race conditions — only process PendingDelete rows.
            if (!Enum.TryParse<DbInstanceStatus>(dbInstance.Status, ignoreCase: true, out var status)
                || status != DbInstanceStatus.PendingDelete)
            {
                return;
            }

            dbInstance.Status = DbInstanceStatus.DeleteInProgress.ToString();
            dbInstance.LastModifiedDate = DateTime.UtcNow;
            dbInstance.LastRefreshed = DateTime.UtcNow;
            await adminApiDbContext.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(dbInstance.DatabaseName))
            {
                await _sandboxProvisioner.DeleteSandboxesAsync(dbInstance.DatabaseName);
            }

            if (dbInstance.OdsInstanceId.HasValue)
            {
                var odsInstance = await resolvedUsersContext.OdsInstances
                    .FindAsync(dbInstance.OdsInstanceId.Value);

                if (odsInstance is not null)
                {
                    resolvedUsersContext.OdsInstances.Remove(odsInstance);
                    await resolvedUsersContext.SaveChangesAsync(CancellationToken.None);
                }
            }

            dbInstance.Status = DbInstanceStatus.Deleted.ToString();
            dbInstance.LastModifiedDate = DateTime.UtcNow;
            dbInstance.LastRefreshed = DateTime.UtcNow;
            await adminApiDbContext.SaveChangesAsync();
        }
        catch
        {
            if (dbInstance is not null)
            {
                dbInstance.Status = DbInstanceStatus.DeleteFailed.ToString();
                dbInstance.LastModifiedDate = DateTime.UtcNow;
                dbInstance.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();
            }

            throw;
        }
        finally
        {
            _tenantConfigurationContextProvider.Set(null);
            tenantUsersContext?.Dispose();

            if (tenantAdminApiDbContext is not null)
            {
                await tenantAdminApiDbContext.DisposeAsync();
            }
        }
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
