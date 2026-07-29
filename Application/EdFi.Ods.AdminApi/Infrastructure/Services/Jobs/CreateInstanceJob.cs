// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Features.OdsInstances.Manage;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Quartz;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class CreateInstanceJob(
    ILogger<CreateInstanceJob> logger,
    IJobStatusService jobStatusService,
    AdminApiDbContext dbContext,
    IUsersContext usersContext,
    ITenantConfigurationProvider tenantConfigurationProvider,
    IContextProvider<TenantConfiguration> tenantConfigurationContextProvider,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    ISymmetricStringEncryptionProvider encryptionProvider,
    ISandboxProvisioner sandboxProvisioner,
    IOptions<AppSettings> options,
    IConfiguration configuration,
    IDbConnectionStringBuilderAdapterFactory connectionStringBuilderAdapterFactory)
    : AdminApiQuartzJobBase(logger, jobStatusService)
{
    private const int MaxSynchronizedNameLength = 100;

    private readonly AdminApiDbContext _dbContext = dbContext;
    private readonly IUsersContext _usersContext = usersContext;
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider = tenantConfigurationProvider;
    private readonly IContextProvider<TenantConfiguration> _tenantConfigurationContextProvider = tenantConfigurationContextProvider;
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider = encryptionProvider;
    private readonly ISandboxProvisioner _sandboxProvisioner = sandboxProvisioner;
    private readonly IOptions<AppSettings> _options = options;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDbConnectionStringBuilderAdapterFactory _connectionStringBuilderAdapterFactory = connectionStringBuilderAdapterFactory;

    internal static JobKey CreateJobKey(int odsInstanceManageId, string? tenantName)
        => new(BuildJobIdentity(odsInstanceManageId, tenantName));

    internal static string BuildJobIdentity(int odsInstanceManageId, string? tenantName)
        => string.IsNullOrWhiteSpace(tenantName)
            ? $"{JobConstants.CreateInstanceJobName}-{odsInstanceManageId}"
            : $"{JobConstants.CreateInstanceJobName}-{tenantName}-{odsInstanceManageId}";

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        if (!context.MergedJobDataMap.ContainsKey(JobConstants.OdsInstanceManageIdKey))
        {
            throw new InvalidOperationException($"{JobConstants.OdsInstanceManageIdKey} must be provided for {JobConstants.CreateInstanceJobName}.");
        }

        var odsInstanceManageId = context.MergedJobDataMap.GetInt(JobConstants.OdsInstanceManageIdKey);
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var tenantName = GetTenantName(context, multiTenancyEnabled);

        // Separate variables for tenant-specific contexts so they can be explicitly disposed in finally.
        // In single-tenant mode these remain null and the injected _dbContext/_usersContext are used directly.
        AdminApiDbContext? tenantAdminApiDbContext = null;
        IUsersContext? tenantUsersContext = null;
        TenantConfiguration? tenantConfiguration = null;
        var adminApiDbContext = _dbContext;
        var resolvedUsersContext = _usersContext;
        OdsInstanceManage? odsInstanceManage = null;

        try
        {
            if (multiTenancyEnabled)
            {
                if (!_tenantConfigurationProvider.Get().TryGetValue(tenantName!, out tenantConfiguration)
                    || tenantConfiguration is null)
                {
                    throw new InvalidOperationException($"Tenant '{tenantName}' is not configured.");
                }

                // Quartz jobs execute outside the HTTP pipeline, so TenantResolverMiddleware never runs.
                // We must set the tenant context manually here so that downstream services that depend
                // on IContextProvider<TenantConfiguration> (e.g. ConfigConnectionStringsProvider) resolve
                // the correct per-tenant connection strings (EdFi_Master, EdFi_Ods, etc.).
                // The tenant name is always known at this point because it was stored in the job data map
                // when CreatePendingOdsInstanceManagesDispatcherJob scheduled this job.
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

            if (!IsEligibleForProcessing(odsInstanceManage))
            {
                return;
            }

            ValidatePendingState(odsInstanceManage);

            var finalName = odsInstanceManage.Name;
            ValidateFinalName(finalName);
            var existingOdsInstance = await GetExistingOdsInstanceByNameAsync(resolvedUsersContext, finalName);

            var now = DateTime.UtcNow;
            odsInstanceManage.Status = OdsInstanceManageStatus.CreateInProgress.ToString();
            if (string.IsNullOrWhiteSpace(odsInstanceManage.DatabaseName))
            {
                odsInstanceManage.DatabaseName = OdsInstanceManageDatabaseNameFormatter.Build(
                    odsInstanceManage.Name,
                    odsInstanceManage.DatabaseTemplate);
            }

            odsInstanceManage.LastModifiedDate = now;
            odsInstanceManage.LastRefreshed = now;
            await adminApiDbContext.SaveChangesAsync();

            await _sandboxProvisioner.AddSandboxAsync(
                odsInstanceManage.DatabaseName,
                GetSandboxType(odsInstanceManage.DatabaseTemplate));

            var encryptedConnectionString = BuildEncryptedConnectionString(odsInstanceManage.DatabaseName, tenantName);

            var odsInstance = existingOdsInstance ?? new OdsInstance
            {
                Name = finalName,
                InstanceType = odsInstanceManage.DatabaseTemplate,
                ConnectionString = encryptedConnectionString
            };

            odsInstance.InstanceType = odsInstanceManage.DatabaseTemplate;
            odsInstance.ConnectionString = encryptedConnectionString;

            if (existingOdsInstance is null)
            {
                resolvedUsersContext.OdsInstances.Add(odsInstance);
            }

            await resolvedUsersContext.SaveChangesAsync(CancellationToken.None);

            odsInstanceManage.OdsInstanceId = odsInstance.OdsInstanceId;
            odsInstanceManage.OdsInstanceName = finalName;
            odsInstanceManage.Status = OdsInstanceManageStatus.Created.ToString();
            odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
            odsInstanceManage.LastRefreshed = DateTime.UtcNow;

            await adminApiDbContext.SaveChangesAsync();
        }
        catch
        {
            if (odsInstanceManage is not null)
            {
                odsInstanceManage.Status = OdsInstanceManageStatus.CreateFailed.ToString();
                odsInstanceManage.LastModifiedDate = DateTime.UtcNow;
                odsInstanceManage.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();
            }

            throw;
        }
        finally
        {
            // Always clear the tenant context regardless of success or failure.
            // This is the job-level equivalent of what TenantResolverMiddleware does at the end of an HTTP request.
            _tenantConfigurationContextProvider.Set(null);
            tenantUsersContext?.Dispose();

            if (tenantAdminApiDbContext is not null)
            {
                await tenantAdminApiDbContext.DisposeAsync();
            }
        }
    }

    private string BuildEncryptedConnectionString(string databaseName, string? tenantName)
    {
        var encryptionKey = _options.Value.EncryptionKey
            ?? throw new InvalidOperationException("EncryptionKey can't be null.");

        var connectionStringBuilderAdapter = _connectionStringBuilderAdapterFactory.Get();
        connectionStringBuilderAdapter.ConnectionString = GetOdsConnectionString(tenantName);
        connectionStringBuilderAdapter.DatabaseName = databaseName;

        return _encryptionProvider.Encrypt(
            connectionStringBuilderAdapter.ConnectionString,
            Convert.FromBase64String(encryptionKey));
    }

    private string GetOdsConnectionString(string? tenantName)
    {
        if (_options.Value.MultiTenancy)
        {
            if (string.IsNullOrWhiteSpace(tenantName))
            {
                throw new InvalidOperationException(
                    $"{JobConstants.TenantNameKey} must be provided when multi-tenancy is enabled.");
            }

            var tenantOdsConnectionString = _configuration[$"Tenants:{tenantName}:ConnectionStrings:EdFi_Ods"];

            if (string.IsNullOrWhiteSpace(tenantOdsConnectionString))
            {
                throw new InvalidOperationException(
                    $"EdFi_Ods connection string is not configured for tenant '{tenantName}'.");
            }

            return tenantOdsConnectionString;
        }

        return _configuration.GetConnectionString("EdFi_Ods")
            ?? throw new InvalidOperationException("EdFi_Ods connection string is not configured.");
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

    private static SandboxType GetSandboxType(string databaseTemplate)
    {
        if (Enum.TryParse<SandboxType>(databaseTemplate, ignoreCase: true, out var sandboxType))
        {
            return sandboxType;
        }

        throw new InvalidOperationException(
            $"DatabaseTemplate '{databaseTemplate}' cannot be mapped to {nameof(SandboxType)}.");
    }

    private static bool IsEligibleForProcessing(OdsInstanceManage odsInstanceManage)
    {
        if (!Enum.TryParse<OdsInstanceManageStatus>(odsInstanceManage.Status, ignoreCase: true, out var status))
        {
            throw new InvalidOperationException(
                $"OdsInstanceManage '{odsInstanceManage.Id}' has unsupported status '{odsInstanceManage.Status}'.");
        }

        return status == OdsInstanceManageStatus.PendingCreate;
    }

    private static void ValidatePendingState(OdsInstanceManage odsInstanceManage)
    {
        if (odsInstanceManage.OdsInstanceId.HasValue || !string.IsNullOrWhiteSpace(odsInstanceManage.OdsInstanceName))
        {
            throw new InvalidOperationException(
                $"OdsInstanceManage '{odsInstanceManage.Id}' is in an invalid pending state because ODS references already exist.");
        }

        if (string.IsNullOrWhiteSpace(odsInstanceManage.DatabaseTemplate))
        {
            throw new InvalidOperationException(
                $"OdsInstanceManage '{odsInstanceManage.Id}' is missing DatabaseTemplate.");
        }
    }

    private static void ValidateFinalName(string finalName)
    {
        if (finalName.Length > MaxSynchronizedNameLength)
        {
            throw new InvalidOperationException(
                $"The synchronized ODS instance name '{finalName}' exceeds the maximum length of {MaxSynchronizedNameLength} characters.");
        }
    }

    private static Task<OdsInstance?> GetExistingOdsInstanceByNameAsync(IUsersContext usersContext, string finalName)
        => usersContext.OdsInstances.FirstOrDefaultAsync(instance => instance.Name == finalName, CancellationToken.None);
}
