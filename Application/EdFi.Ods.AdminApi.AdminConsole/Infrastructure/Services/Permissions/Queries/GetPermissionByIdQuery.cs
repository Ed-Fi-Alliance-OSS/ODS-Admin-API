// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Queries;

public interface IGetPermissionByIdQuery
{
    Task<Permission> Execute(int tenantId, int DocId);
}

public class GetPermissionByIdQuery : IGetPermissionByIdQuery
{
    private readonly IQueriesRepository<Permission> _permissionQuery;

    public GetPermissionByIdQuery(IQueriesRepository<Permission> permissionQuery)
    {
        _permissionQuery = permissionQuery;
    }

    public async Task<Permission> Execute(int tenantId, int DocId)
    {
        var permission = await _permissionQuery.Query().SingleOrDefaultAsync(permission => permission.TenantId == tenantId && permission.DocId == DocId);
        return permission;
    }
}
