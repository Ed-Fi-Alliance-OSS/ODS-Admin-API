// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
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
