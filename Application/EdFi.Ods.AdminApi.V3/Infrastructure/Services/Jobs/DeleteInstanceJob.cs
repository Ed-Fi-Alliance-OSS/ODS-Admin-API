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

    internal static JobKey CreateJobKey(int odsInstanceManageId, string? tenantName)
        => new(BuildJobIdentity(odsInstanceManageId, tenantName));

    internal static string BuildJobIdentity(int odsInstanceManageId, string? tenantName)
        => string.IsNullOrWhiteSpace(tenantName)
            ? $"{JobConstants.DeleteInstanceJobName}-{odsInstanceManageId}"
            : $"{JobConstants.DeleteInstanceJobName}-{tenantName}-{odsInstanceManageId}";

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        if (!context.MergedJobDataMap.ContainsKey(JobConstants.OdsInstanceManageIdKey))
        {
            throw new InvalidOperationException($"{JobConstants.OdsInstanceManageIdKey} must be provided for {JobConstants.DeleteInstanceJobName}.");
        }

        var odsInstanceManageId = context.MergedJobDataMap.GetInt(JobConstants.OdsInstanceManageIdKey);
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var tenantName = GetTenantName(context, multiTenancyEnabled);

        AdminApiDbContext? tenantAdminApiDbContext = null;
        IUsersContext? tenantUsersContext = null;
        var adminApiDbContext = _dbContext;
        var resolvedUsersContext = _usersContext;
        OdsInstanceManage? odsInstanceManage = null;

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

            odsInstanceManage = await adminApiDbContext.OdsInstanceManages
                .FirstOrDefaultAsync(instance => instance.Id == odsInstanceManageId);

            if (odsInstanceManage is null)
            {
                throw new InvalidOperationException($"OdsInstanceManage '{odsInstanceManageId}' was not found.");
            }

            // Guard against race conditions — only process PendingDelete rows.
            if (!Enum.TryParse<OdsInstanceManageStatus>(odsInstanceManage.Status, ignoreCase: true, out var status)
                || status != OdsInstanceManageStatus.PendingDelete)
            {
                return;
            }

            odsInstanceManage.Status = OdsInstanceManageStatus.DeleteInProgress.ToString();
            odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
            odsInstanceManage.LastRefreshed = DateTime.UtcNow;
            await adminApiDbContext.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(odsInstanceManage.DatabaseName))
            {
                await _sandboxProvisioner.DeleteSandboxesAsync(odsInstanceManage.DatabaseName);
            }

            if (odsInstanceManage.OdsInstanceId.HasValue)
            {
                var dataStore = await resolvedUsersContext.OdsInstances
                    .FindAsync(odsInstanceManage.OdsInstanceId.Value);

                if (dataStore is not null)
                {
                    resolvedUsersContext.OdsInstances.Remove(dataStore);
                    await resolvedUsersContext.SaveChangesAsync(CancellationToken.None);
                }
            }

            odsInstanceManage.Status = OdsInstanceManageStatus.Deleted.ToString();
            odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
            odsInstanceManage.LastRefreshed = DateTime.UtcNow;
            await adminApiDbContext.SaveChangesAsync();
        }
        catch
        {
            if (odsInstanceManage is not null)
            {
                odsInstanceManage.Status = OdsInstanceManageStatus.DeleteFailed.ToString();
                odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                odsInstanceManage.LastRefreshed = DateTime.UtcNow;
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
