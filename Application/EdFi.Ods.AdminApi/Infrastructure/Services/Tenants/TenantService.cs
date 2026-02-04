// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Constants = EdFi.Ods.AdminApi.Common.Constants.Constants;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;

public interface ITenantsService
{
    Task InitializeTenantsAsync();
    Task<List<TenantModel>> GetTenantsAsync(bool fromCache = false);
    Task<TenantModel?> GetTenantByTenantIdAsync(string tenantName);
    Task<TenantDetailModel?> GetTenantDetailsAsync(IGetOdsInstancesQuery getOdsInstancesQuery, IGetEducationOrganizationQuery getEducationOrganizationQuery, IMapper mapper, string tenantName);
}

public class TenantService(IOptionsSnapshot<AppSettingsFile> options,
    IMemoryCache memoryCache) : ITenantsService
{
    private const string ADMIN_DB_KEY = "EdFi_Admin";
    private const string SECURITY_DB_KEY = "EdFi_Security";
    protected AppSettingsFile _appSettings = options.Value;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private static readonly ILog _log = LogManager.GetLogger(typeof(TenantService));

    public async Task InitializeTenantsAsync()
    {
        var tenants = await GetTenantsAsync();
        //store it in memorycache
        await Task.FromResult(_memoryCache.Set(Constants.TenantsCacheKey, tenants));
    }

    public async Task<List<TenantModel>> GetTenantsAsync(bool fromCache = false)
    {
        List<TenantModel> results;

        if (fromCache)
        {
            results = await GetTenantsFromCacheAsync();
            if (results.Count > 0)
            {
                return results;
            }
        }

        results = [];

        if (_appSettings.AppSettings.MultiTenancy)
        {
            foreach (var tenantConfig in _appSettings.Tenants)
            {
                /// Admin database
                var adminConnectionString = tenantConfig.Value.ConnectionStrings.First(p => p.Key == ADMIN_DB_KEY).Value;
                if (!ConnectionStringHelper.ValidateConnectionString(_appSettings.AppSettings.DatabaseEngine!, adminConnectionString))
                {
                    _log.WarnFormat("Tenant {Key} has an invalid connection string for database {ADMIN_DB_KEY}. Database engine is {engine}",
                        tenantConfig.Key, ADMIN_DB_KEY, _appSettings.AppSettings.DatabaseEngine);
                }

                /// Security database
                var securityConnectionString = tenantConfig.Value.ConnectionStrings.First(p => p.Key == SECURITY_DB_KEY).Value;
                if (!ConnectionStringHelper.ValidateConnectionString(_appSettings.AppSettings.DatabaseEngine!, securityConnectionString))
                {
                    _log.WarnFormat("Tenant {Key} has an invalid connection string for database {SECURITY_DB_KEY}. Database engine is {engine}",
                        tenantConfig.Key, SECURITY_DB_KEY, _appSettings.AppSettings.DatabaseEngine);
                }

                results.Add(new TenantModel()
                {
                    TenantName = tenantConfig.Key,
                    ConnectionStrings = new(adminConnectionString, securityConnectionString)
                });
            }
        }
        else
        {
            results.Add(new TenantModel()
            {
                TenantName = Constants.DefaultTenantName,
                ConnectionStrings = new TenantModelConnectionStrings
                (
                    edFiAdminConnectionString: _appSettings.ConnectionStrings.First(p => p.Key == ADMIN_DB_KEY).Value,
                    edFiSecurityConnectionString: _appSettings.ConnectionStrings.First(p => p.Key == SECURITY_DB_KEY).Value
                )
            });
        }

        return results;
    }

    public async Task<TenantModel?> GetTenantByTenantIdAsync(string tenantName)
    {
        var tenants = await GetTenantsAsync();
        var tenant = tenants.FirstOrDefault(p => p.TenantName.Equals(tenantName, StringComparison.OrdinalIgnoreCase));
        return tenant;
    }

    public async Task<TenantDetailModel?> GetTenantDetailsAsync(
        IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IMapper mapper,
        string tenantName)
    {
        var tenant = await GetTenantByTenantIdAsync(tenantName);

        if (tenant is not null)
        {
            var tenantDetails = new TenantDetailModel() { TenantName = tenantName };

            var odsInstances = getOdsInstancesQuery.Execute();

            tenantDetails.OdsInstances = mapper.Map<List<TenantOdsInstanceModel>>(odsInstances);

            var OdsInstanceIdsList = tenantDetails.OdsInstances.Select(i => i.OdsInstanceId).ToArray();

            if (OdsInstanceIdsList is not null && OdsInstanceIdsList.Length > 0)
            {
                var edOrgsList = getEducationOrganizationQuery.Execute(OdsInstanceIdsList);

                foreach (var odsInstance in tenantDetails.OdsInstances)
                {
                    var edOrgs = edOrgsList.Where(eo => eo.InstanceId == odsInstance.OdsInstanceId).ToList();
                    odsInstance.EducationOrganizations = mapper.Map<List<TenantEducationOrganizationModel>>(edOrgs);
                }
            }

            return tenantDetails;
        }

        return null;
    }

    private async Task<List<TenantModel>> GetTenantsFromCacheAsync()
    {
        var tenants = await Task.FromResult(_memoryCache.Get<List<TenantModel>>(Constants.TenantsCacheKey));
        return tenants ?? [];
    }
}
