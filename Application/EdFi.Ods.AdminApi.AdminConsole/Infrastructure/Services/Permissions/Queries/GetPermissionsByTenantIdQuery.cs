// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Queries;

public interface IGetPermissionsByTenantIdQuery
{
    Task<IEnumerable<Permission>> Execute(int tenantId);
}

public class GetPermissionsByTenantIdQuery : IGetPermissionsByTenantIdQuery
{
    private readonly IQueriesRepository<Permission> _permissionQuery;

    public GetPermissionsByTenantIdQuery(IQueriesRepository<Permission> permissionQuery)
    {
        _permissionQuery = permissionQuery;
    }

    public async Task<IEnumerable<Permission>> Execute(int tenantId)
    {
        var permission = await _permissionQuery.Query().Where(permission => permission.TenantId == tenantId).ToListAsync();
        return permission.ToList();
    }
}
