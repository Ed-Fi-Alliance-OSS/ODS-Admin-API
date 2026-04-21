// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetOdsInstanceIdsByApplicationIdQueryBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected IList<int> ExecuteCore(int applicationId)
    {
        return _context.ApiClientOdsInstances
            .Where(p => p.ApiClient.Application.ApplicationId == applicationId)
            .Select(p => p.OdsInstance.OdsInstanceId)
            .Distinct()
            .ToList();
    }

    protected IReadOnlyDictionary<int, IList<int>> ExecuteCore(IEnumerable<int> applicationIds)
    {
        var ids = applicationIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, IList<int>>();
        }

        return _context.ApiClientOdsInstances
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
    }
}
