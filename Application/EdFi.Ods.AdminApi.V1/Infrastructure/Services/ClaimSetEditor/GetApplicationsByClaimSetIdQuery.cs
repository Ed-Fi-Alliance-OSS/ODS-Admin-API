// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class GetApplicationsByClaimSetIdQuery : IGetApplicationsByClaimSetIdQuery
{
    private readonly ISecurityContext _securityContext;
    private readonly IUsersContext _usersContext;

    public GetApplicationsByClaimSetIdQuery(ISecurityContext securityContext, IUsersContext usersContext)
    {
        _securityContext = securityContext;
        _usersContext = usersContext;
    }

    public IEnumerable<Application> Execute(int claimSetId)
    {
        var claimSetName = GetClaimSetNameById(claimSetId);

        return GetApplicationsByClaimSetName(claimSetName);
    }

    private string GetClaimSetNameById(int claimSetId)
    {
        return _securityContext.ClaimSets
            .Select(x => new { x.ClaimSetId, x.ClaimSetName })
            .Single(x => x.ClaimSetId == claimSetId).ClaimSetName;
    }

    private IEnumerable<Application> GetApplicationsByClaimSetName(string claimSetName)
    {
        return _usersContext.Applications
            .Where(x => x.ClaimSetName == claimSetName)
            .OrderBy(x => x.ClaimSetName)
            .Select(x => new Application
            {
                Name = x.ApplicationName,
                VendorName = x.Vendor != null ? x.Vendor.VendorName : null
            })
            .ToList();
    }

    public int ExecuteCount(int claimSetId)
    {
        return Execute(claimSetId).Count();
    }
}

public interface IGetApplicationsByClaimSetIdQuery
{
    IEnumerable<Application> Execute(int securityContextClaimSetId);

    int ExecuteCount(int claimSetId);
}
