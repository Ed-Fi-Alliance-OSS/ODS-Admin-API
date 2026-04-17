// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Features.DbInstances;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
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
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider = encryptionProvider;
    private readonly ISandboxProvisioner _sandboxProvisioner = sandboxProvisioner;
    private readonly IOptions<AppSettings> _options = options;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDbConnectionStringBuilderAdapterFactory _connectionStringBuilderAdapterFactory = connectionStringBuilderAdapterFactory;

    internal static JobKey CreateJobKey(int dbInstanceId, string? tenantName)
        => new(BuildJobIdentity(dbInstanceId, tenantName));

    internal static string BuildJobIdentity(int dbInstanceId, string? tenantName)
        => string.IsNullOrWhiteSpace(tenantName)
            ? $"{JobConstants.CreateInstanceJobName}-{dbInstanceId}"
            : $"{JobConstants.CreateInstanceJobName}-{tenantName}-{dbInstanceId}";

    protected override async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        if (!context.MergedJobDataMap.ContainsKey(JobConstants.DbInstanceIdKey))
        {
            throw new InvalidOperationException($"{JobConstants.DbInstanceIdKey} must be provided for {JobConstants.CreateInstanceJobName}.");
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

            if (!IsEligibleForProcessing(dbInstance))
            {
                return;
            }

            ValidatePendingState(dbInstance);

            var finalName = dbInstance.Name;
            ValidateFinalName(finalName);
            var existingOdsInstance = await GetExistingOdsInstanceByNameAsync(resolvedUsersContext, finalName);

            var now = DateTime.UtcNow;
            dbInstance.Status = DbInstanceStatus.InProgress.ToString();
            if (string.IsNullOrWhiteSpace(dbInstance.DatabaseName))
            {
                dbInstance.DatabaseName = DbInstanceDatabaseNameFormatter.Build(
                    dbInstance.Name,
                    dbInstance.DatabaseTemplate);
            }

            dbInstance.LastModifiedDate = now;
            dbInstance.LastRefreshed = now;
            await adminApiDbContext.SaveChangesAsync();

            await _sandboxProvisioner.AddSandboxAsync(
                dbInstance.DatabaseName,
                GetSandboxType(dbInstance.DatabaseTemplate));

            var encryptedConnectionString = BuildEncryptedConnectionString(dbInstance.DatabaseName, tenantName);

            var odsInstance = existingOdsInstance ?? new OdsInstance
            {
                Name = finalName,
                InstanceType = dbInstance.DatabaseTemplate,
                ConnectionString = encryptedConnectionString
            };

            odsInstance.InstanceType = dbInstance.DatabaseTemplate;
            odsInstance.ConnectionString = encryptedConnectionString;

            if (existingOdsInstance is null)
            {
                resolvedUsersContext.OdsInstances.Add(odsInstance);
            }

            await resolvedUsersContext.SaveChangesAsync(CancellationToken.None);

            dbInstance.OdsInstanceId = odsInstance.OdsInstanceId;
            dbInstance.OdsInstanceName = finalName;
            dbInstance.Status = DbInstanceStatus.Completed.ToString();
            dbInstance.LastModifiedDate = DateTime.UtcNow;
            dbInstance.LastRefreshed = DateTime.UtcNow;

            await adminApiDbContext.SaveChangesAsync();
        }
        catch
        {
            if (dbInstance is not null)
            {
                dbInstance.Status = DbInstanceStatus.Error.ToString();
                dbInstance.LastModifiedDate = DateTime.UtcNow;
                dbInstance.LastRefreshed = DateTime.UtcNow;
                await adminApiDbContext.SaveChangesAsync();
            }

            throw;
        }
        finally
        {
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

    private static bool IsEligibleForProcessing(DbInstance dbInstance)
    {
        if (!Enum.TryParse<DbInstanceStatus>(dbInstance.Status, ignoreCase: true, out var status))
        {
            throw new InvalidOperationException(
                $"DbInstance '{dbInstance.Id}' has unsupported status '{dbInstance.Status}'.");
        }

        return status == DbInstanceStatus.Pending;
    }

    private static void ValidatePendingState(DbInstance dbInstance)
    {
        if (dbInstance.OdsInstanceId.HasValue || !string.IsNullOrWhiteSpace(dbInstance.OdsInstanceName))
        {
            throw new InvalidOperationException(
                $"DbInstance '{dbInstance.Id}' is in an invalid pending state because ODS references already exist.");
        }

        if (string.IsNullOrWhiteSpace(dbInstance.DatabaseTemplate))
        {
            throw new InvalidOperationException(
                $"DbInstance '{dbInstance.Id}' is missing DatabaseTemplate.");
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
