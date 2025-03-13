// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Queries;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Permissions;

public class ReadPermissions : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/permissions", GetPermissions)
            .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/permissions/{tenantId}/{id}", GetPermissionById)
            .WithRouteOptions(b => b.WithResponse<PermissionModel>(200))
            .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/permissions/{tenantId}", GetPermissionsByTenantId)
            .WithRouteOptions(b => b.WithResponse<PermissionModel>(200))
            .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    internal static async Task<IResult> GetPermissions([FromServices] IGetPermissionsQuery getPermissionQuery)
    {
        var permissions = await getPermissionQuery.Execute();
        return Results.Ok(permissions);
    }

    internal static async Task<IResult> GetPermissionById([FromServices] IGetPermissionByIdQuery getPermissionQuery, int tenantId, int id)
    {
        var permission = await getPermissionQuery.Execute(tenantId, id);

        if (permission != null)
            return Results.Ok(permission);

        return Results.NotFound();
    }

    internal static async Task<IResult> GetPermissionsByTenantId([FromServices] IGetPermissionsByTenantIdQuery getPermissionQuery, int tenantId)
    {
        var permissions = await getPermissionQuery.Execute(tenantId);
        if (permissions.Any())
        {
            return Results.Ok(permissions);
        }
        return Results.NotFound();
    }
}
