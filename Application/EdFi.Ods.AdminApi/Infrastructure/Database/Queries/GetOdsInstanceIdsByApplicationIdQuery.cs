// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstanceIdsByApplicationIdQuery
{
    IList<int> Execute(int applicationId);

    IReadOnlyDictionary<int, IList<int>> Execute(IEnumerable<int> applicationIds);
}

public class GetOdsInstanceIdsByApplicationIdQuery : IGetOdsInstanceIdsByApplicationIdQuery
{
    private readonly IUsersContext _context;

    public GetOdsInstanceIdsByApplicationIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public IList<int> Execute(int applicationId)
    {
        return _context.ApiClientOdsInstances
            .Where(p => p.ApiClient.Application.ApplicationId == applicationId)
            .Select(p => p.OdsInstance.OdsInstanceId)
            .Distinct()
            .ToList();
    }

    public IReadOnlyDictionary<int, IList<int>> Execute(IEnumerable<int> applicationIds)
    {
        var ids = applicationIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, IList<int>>();
        }

        var groupedOdsInstanceIds = _context.ApiClientOdsInstances
            .Where(p => ids.Contains(p.ApiClient.Application.ApplicationId))
            .Select(p => new
            {
                ApplicationId = p.ApiClient.Application.ApplicationId,
                p.OdsInstance.OdsInstanceId
            })
            .Distinct()
            .AsEnumerable()
            .GroupBy(x => x.ApplicationId)
            .ToDictionary(
                group => group.Key,
                group => (IList<int>)group.Select(x => x.OdsInstanceId).ToList());

        return groupedOdsInstanceIds;
    }
}
