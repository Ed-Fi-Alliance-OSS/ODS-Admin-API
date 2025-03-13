// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Queries;

public interface IGetPermissionsQuery
{
    Task<IEnumerable<Permission>> Execute();
}

public class GetPermissionsQuery(IQueriesRepository<Permission> permissionQuery) : IGetPermissionsQuery
{
    private readonly IQueriesRepository<Permission> _permissionQuery = permissionQuery;

    public async Task<IEnumerable<Permission>> Execute()
    {
        var permissions = await _permissionQuery.GetAllAsync();

        return permissions.ToList();
    }
}
