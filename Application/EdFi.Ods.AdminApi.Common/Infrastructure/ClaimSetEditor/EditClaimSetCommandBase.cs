// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

public interface IEditClaimSetModelCommon
{
    string? ClaimSetName { get; }
    int ClaimSetId { get; }
}

public abstract class EditClaimSetCommandBase(ISecurityContext securityContext, IUsersContext usersContext)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IUsersContext _usersContext = usersContext;

    protected int ExecuteCore(IEditClaimSetModelCommon claimSet)
    {
        var existingClaimSet = _securityContext.ClaimSets.Single(x => x.ClaimSetId == claimSet.ClaimSetId);

        if (existingClaimSet.ForApplicationUseOnly || existingClaimSet.IsEdfiPreset)
        {
            throw new AdminApiException($"Claim set ({existingClaimSet.ClaimSetName}) is system reserved. May not be modified.");
        }

        if (claimSet.ClaimSetName is null)
        {
            throw new InvalidOperationException("Cannot have a null ClaimSetName");
        }

        if (!claimSet.ClaimSetName.Equals(existingClaimSet.ClaimSetName, StringComparison.InvariantCultureIgnoreCase))
        {
            ReAssociateApplicationsToRenamedClaimSet(existingClaimSet.ClaimSetName, claimSet.ClaimSetName);
        }

        existingClaimSet.ClaimSetName = claimSet.ClaimSetName;

        _securityContext.SaveChanges();
        _usersContext.SaveChanges();

        return existingClaimSet.ClaimSetId;
    }

    private void ReAssociateApplicationsToRenamedClaimSet(string existingClaimSetName, string newClaimSetName)
    {
        var associatedApplications = _usersContext.Applications
            .Where(x => x.ClaimSetName == existingClaimSetName);

        foreach (var application in associatedApplications)
        {
            application.ClaimSetName = newClaimSetName;
        }
    }
}
