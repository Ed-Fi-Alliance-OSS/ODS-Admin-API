// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

public abstract class GetApplicationsByClaimSetIdQueryBase(ISecurityContext securityContext, IUsersContext usersContext)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IUsersContext _usersContext = usersContext;

    protected IEnumerable<ApplicationModel> ExecuteCore(int securityContextClaimSetId)
    {
        var claimSetName = GetClaimSetNameById(securityContextClaimSetId);

        return GetApplicationsByClaimSetName(claimSetName);
    }

    protected int ExecuteCountCore(int claimSetId)
    {
        return ExecuteCore(claimSetId).Count();
    }

    private string GetClaimSetNameById(int claimSetId)
    {
        return _securityContext.ClaimSets
            .Select(x => new { x.ClaimSetId, x.ClaimSetName })
            .Single(x => x.ClaimSetId == claimSetId)
            .ClaimSetName;
    }

    private IEnumerable<ApplicationModel> GetApplicationsByClaimSetName(string claimSetName)
    {
        return _usersContext.Applications
            .Where(x => x.ClaimSetName == claimSetName)
            .OrderBy(x => x.ClaimSetName)
            .Select(x => new ApplicationModel
            {
                Name = x.ApplicationName,
                VendorName = x.Vendor.VendorName
            })
            .ToList();
    }
}
