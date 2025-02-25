// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;

public interface IGetInstancesByTenantIdQuery
{
    Task<IEnumerable<Instance>> Execute(int tenantId);
}

public class GetInstancesByTenantIdQuery : IGetInstancesByTenantIdQuery
{
    private readonly IQueriesRepository<Instance> _instanceQuery;

    public GetInstancesByTenantIdQuery(IQueriesRepository<Instance> instanceQuery)
    {
        _instanceQuery = instanceQuery;
    }

    public async Task<IEnumerable<Instance>> Execute(int tenantId)
    {

        var instances = await _instanceQuery.Query().Where(instance => instance.TenantId == tenantId).ToListAsync();
        return instances.ToList();
    }
}
