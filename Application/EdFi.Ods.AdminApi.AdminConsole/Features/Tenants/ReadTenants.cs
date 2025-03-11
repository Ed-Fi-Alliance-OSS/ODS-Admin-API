// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore.Filters;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Tenants;

public class ReadTenants : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/tenants", GetTenantsAsync)
            .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/tenants/{tenantId}", GetTenantsByTenantIdAsync)
           .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public async Task<IResult> GetTenantsAsync(IAdminConsoleTenantsService adminConsoleTenantsService, IMemoryCache memoryCache)
    {
        var tenants = await adminConsoleTenantsService.GetTenantsAsync(true);
        return Results.Ok(tenants);
    }

    public async Task<IResult> GetTenantsByTenantIdAsync(IAdminConsoleTenantsService adminConsoleTenantsService,
        IMemoryCache memoryCache, int tenantId)
    {
        var tenant = await adminConsoleTenantsService.GetTenantByTenantIdAsync(tenantId);
        if (tenant != null)
            return Results.Ok(tenant);
        return Results.NotFound();
    }
}
