// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstanceIdsByApiClientIdQuery
{
    IList<int> Execute(int apiClientId);

    IReadOnlyDictionary<int, IList<int>> Execute(IEnumerable<int> apiClientIds);
}

public class GetOdsInstanceIdsByApiClientIdQuery : IGetOdsInstanceIdsByApiClientIdQuery
{
    private readonly IUsersContext _context;

    public GetOdsInstanceIdsByApiClientIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public IList<int> Execute(int apiClientId)
    {
        return _context.ApiClientOdsInstances
            .Where(p => p.ApiClient.ApiClientId == apiClientId)
            .Select(p => p.OdsInstance.OdsInstanceId)
            .ToList();
    }

    public IReadOnlyDictionary<int, IList<int>> Execute(IEnumerable<int> apiClientIds)
    {
        var ids = apiClientIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, IList<int>>();
        }

        var groupedOdsInstanceIds = _context.ApiClientOdsInstances
            .Where(p => ids.Contains(p.ApiClient.ApiClientId))
            .Select(p => new
            {
                ApiClientId = p.ApiClient.ApiClientId,
                p.OdsInstance.OdsInstanceId
            })
            .Distinct()
            .AsEnumerable()
            .GroupBy(x => x.ApiClientId)
            .ToDictionary(
                group => group.Key,
                group => (IList<int>)group.Select(x => x.OdsInstanceId).ToList());

        return groupedOdsInstanceIds;
    }
}
