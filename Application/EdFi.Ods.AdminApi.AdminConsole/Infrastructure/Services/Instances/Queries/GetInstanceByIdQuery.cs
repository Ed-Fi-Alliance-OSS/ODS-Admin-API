// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;

public interface IGetInstanceByIdQuery
{
    Task<Instance?> Execute(int id);
}

public class GetInstanceByIdQuery(IQueriesRepository<Instance> instanceQuery) : IGetInstanceByIdQuery
{
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;

    public async Task<Instance?> Execute(int id)
    {
        var instance = await _instanceQuery.Query()
            .Include(i => i.OdsInstanceContexts)
            .Include(i => i.OdsInstanceDerivatives)
            .SingleOrDefaultAsync(instance => instance.Id == id);

        if (instance == null)
            return null;

        return instance;
    }
}
