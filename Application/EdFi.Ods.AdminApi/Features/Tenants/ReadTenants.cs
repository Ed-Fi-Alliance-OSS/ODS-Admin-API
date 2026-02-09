// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Constants;
using Constants = EdFi.Ods.AdminApi.Common.Constants.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FluentValidation;
using FluentValidation.Results;
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
            .MapGet(endpoints, "/tenants/{tenantName}/edOrgsByInstances", GetTenantEdOrgsByInstancesAsync)
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
        if (tenant is null)
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

    public static async Task<IResult> GetTenantEdOrgsByInstancesAsync(
        HttpRequest request,
        [FromServices] ITenantsService tenantsService,
        IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IMapper mapper,
        IMemoryCache memoryCache,
        IOptions<AppSettings> options,
        string tenantName
    )
    {
        if (options.Value.MultiTenancy)
        {
            var tenantHeader = request.Headers["tenant"].FirstOrDefault();

            if (tenantHeader is null)
                throw new ValidationException([new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_MissingHeader)]);

            if (!string.Equals(tenantName, tenantHeader, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException([new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_ParameterMismatch)]);
        }
        else if (!string.Equals(tenantName, Constants.DefaultTenantName, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException<string>("TenantName", tenantName);
        }

        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(getOdsInstancesQuery, getEducationOrganizationQuery, mapper, tenantName);

        if (tenant is null)
            return Results.NotFound();

        return Results.Ok(
            new TenantDetailsResponse
            {
                Id = tenant.TenantName,
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
