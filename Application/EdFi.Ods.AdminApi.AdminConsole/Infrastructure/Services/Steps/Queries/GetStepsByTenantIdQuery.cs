// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Queries;

public interface IGetStepsByTenantIdQuery
{
    Task<IEnumerable<Step>> Execute(int docId);
}

public class GetStepsByTenantIdQuery : IGetStepsByTenantIdQuery
{
    private readonly IQueriesRepository<Step> _stepQuery;

    public GetStepsByTenantIdQuery(IQueriesRepository<Step> stepQuery)
    {
        _stepQuery = stepQuery;
    }

    public async Task<IEnumerable<Step>> Execute(int docId)
    {
        var step = await _stepQuery.Query().Where(step => step.DocId == docId).ToListAsync();
        return step;
    }
}
