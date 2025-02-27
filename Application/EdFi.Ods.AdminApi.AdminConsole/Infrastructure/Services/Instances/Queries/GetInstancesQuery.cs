// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Helpers;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;

public interface IGetInstancesQuery
{
    Task<IEnumerable<Instance>> Execute(string? status);
}

public class GetInstancesQuery : IGetInstancesQuery
{
    private readonly IQueriesRepository<Instance> _instanceQuery;

    public GetInstancesQuery(IQueriesRepository<Instance> instanceQuery)
    {
        _instanceQuery = instanceQuery;
    }
    public async Task<IEnumerable<Instance>> Execute(string? status)
    {
        InstanceStatus instanceStatus = InstanceStatus.Pending;
        if (status != null)
        {
            Enum.TryParse(status, true, out instanceStatus);
        }

        return await _instanceQuery.Query()
            .Where(i => status == null || i.Status.Equals(instanceStatus))
            .Include(i => i.OdsInstanceContexts)
            .Include(i => i.OdsInstanceDerivatives)
            .AsNoTracking()
            .ToListAsync();
    }
}
