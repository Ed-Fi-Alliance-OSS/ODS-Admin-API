// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.HealthChecks.Queries;

public interface IGetHealthChecksQuery
{
    Task<IEnumerable<HealthCheck>> Execute();
}

public class GetHealthChecksQuery(IQueriesRepository<HealthCheck> healthCheckQuery) : IGetHealthChecksQuery
{
    private readonly IQueriesRepository<HealthCheck> _healthCheckQuery = healthCheckQuery;

    public async Task<IEnumerable<HealthCheck>> Execute()
    {
        return await _healthCheckQuery.GetAllAsync();
    }
}
