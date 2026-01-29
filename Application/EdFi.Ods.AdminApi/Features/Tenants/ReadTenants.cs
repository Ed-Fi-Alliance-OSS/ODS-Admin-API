// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public class ReadTenants : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/tenants", GetTenantsAsync)
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder
            .MapGet(endpoints, "/tenants/{tenantName}", GetTenantsByTenantIdAsync)
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder
            .MapGet(endpoints, "/tenants/{tenantName}/details", GetTenantDetailsByNameAsync)
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetTenantsAsync(
        [FromServices] ITenantsService tenantsService,
        IMemoryCache memoryCache,
        IOptions<AppSettings> options
    )
    {
        var _databaseEngine =
            options.Value.DatabaseEngine
            ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

        var tenants = await tenantsService.GetTenantsAsync(true);

        var response = tenants
            .Select(t =>
            {
                var adminHostAndDatabase = ConnectionStringHelper.GetHostAndDatabase(
                    _databaseEngine,
                    t.ConnectionStrings.EdFiAdminConnectionString
                );
                var securityHostAndDatabase = ConnectionStringHelper.GetHostAndDatabase(
                    _databaseEngine,
                    t.ConnectionStrings.EdFiSecurityConnectionString
                );

                return new TenantsResponse
                {
                    TenantName = t.TenantName,
                    AdminConnectionString = new EdfiConnectionString()
                    {
                        host = adminHostAndDatabase.Host,
                        database = adminHostAndDatabase.Database
                    },
                    SecurityConnectionString = new EdfiConnectionString()
                    {
                        host = securityHostAndDatabase.Host,
                        database = securityHostAndDatabase.Database
                    }
                };
            })
            .ToList();

        return Results.Ok(response);
    }

    public static async Task<IResult> GetTenantsByTenantIdAsync(
        [FromServices] ITenantsService tenantsService,
        IMemoryCache memoryCache,
        string tenantName,
        IOptions<AppSettings> options
    )
    {
        var _databaseEngine =
            options.Value.DatabaseEngine
            ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

        var tenant = await tenantsService.GetTenantByTenantIdAsync(tenantName);
        if (tenant == null)
            return Results.NotFound();

        var adminHostAndDatabase = ConnectionStringHelper.GetHostAndDatabase(
            _databaseEngine,
            tenant.ConnectionStrings.EdFiAdminConnectionString
        );
        var securityHostAndDatabase = ConnectionStringHelper.GetHostAndDatabase(
            _databaseEngine,
            tenant.ConnectionStrings.EdFiSecurityConnectionString
        );

        return Results.Ok(
            new TenantsResponse
            {
                TenantName = tenant.TenantName,
                AdminConnectionString = new EdfiConnectionString()
                {
                    host = adminHostAndDatabase.Host,
                    database = adminHostAndDatabase.Database
                },
                SecurityConnectionString = new EdfiConnectionString()
                {
                    host = securityHostAndDatabase.Host,
                    database = securityHostAndDatabase.Database
                }
            }
        );
    }

    public static async Task<IResult> GetTenantDetailsByNameAsync(
        [FromServices] ITenantsService tenantsService,
        IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IContextProvider<TenantConfiguration> tenantConfigurationContextProvider,
        IMapper mapper,
        IMemoryCache memoryCache,
        string tenantName,
        IOptions<AppSettings> options
    )
    {
        var tenant = await tenantsService.GetTenantDetailsByNameAsync(
            getOdsInstancesQuery,
            getEducationOrganizationQuery,
            tenantConfigurationContextProvider,
            mapper,
            tenantName);

        if (tenant == null)
            return Results.NotFound();

        return Results.Ok(
            new TenantDetailsResponse
            {
                Id = tenantName,
                Name = tenant.TenantName,
                OdsInstances = tenant.OdsInstances
            }
        );
    }
}

public class TenantsResponse
{
    public string? TenantName { get; set; }
    public EdfiConnectionString? AdminConnectionString { get; set; }
    public EdfiConnectionString? SecurityConnectionString { get; set; }
}

public class EdfiConnectionString
{
    public string? host { get; set; }
    public string? database { get; set; }
}

public class TenantDetailsResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<TenantOdsInstanceModel>? OdsInstances { get; set; }
}

public class OdsInstanceDto
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? InstanceType { get; set; }
    public List<EducationOrganizationDto>? EdOrgs { get; set; }
}

public class EducationOrganizationDto
{
    public int InstanceId { get; set; }
    public string? InstanceName { get; set; }
    public int EducationOrganizationId { get; set; }
    public string? NameOfInstitution { get; set; }
    public string? ShortNameOfInstitution { get; set; }
    public string? Discriminator { get; set; }
    public int? ParentId { get; set; }
}
