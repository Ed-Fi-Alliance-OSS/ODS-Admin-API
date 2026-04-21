// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

public interface IUpdateResourcesOnClaimSetModelCommon
{
    int ClaimSetId { get; }
    List<ResourceClaim>? ResourceClaims { get; }
}

public interface IAddOrEditResourcesOnClaimSetCommandCommon
{
    void Execute(int claimSetId, List<ResourceClaim> resources);
}

public abstract class UpdateResourcesOnClaimSetCommandBase(
    ISecurityContext context,
    IAddOrEditResourcesOnClaimSetCommandCommon addOrEditResourcesOnClaimSetCommand)
{
    protected readonly ISecurityContext _context = context;
    protected readonly IAddOrEditResourcesOnClaimSetCommandCommon _addOrEditResourcesOnClaimSetCommand = addOrEditResourcesOnClaimSetCommand;

    protected void ExecuteCore(int claimSetId, List<ResourceClaim>? resourceClaims)
    {
        var resourceClaimsForClaimSet =
            _context.ClaimSetResourceClaimActions.Where(x => x.ClaimSet.ClaimSetId == claimSetId).ToList();
        _context.ClaimSetResourceClaimActions.RemoveRange(resourceClaimsForClaimSet);
        _context.SaveChanges();

        if (resourceClaims == null)
            return;

        _addOrEditResourcesOnClaimSetCommand.Execute(claimSetId, resourceClaims);
    }
}
