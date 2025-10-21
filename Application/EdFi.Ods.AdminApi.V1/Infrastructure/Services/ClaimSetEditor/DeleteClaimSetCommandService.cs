// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
public class DeleteClaimSetCommandService
{
    private readonly ISecurityContext _context;

    public DeleteClaimSetCommandService(ISecurityContext context)
    {
        _context = context;
    }

    public void Execute(IDeleteClaimSetModel claimSet)
    {
        var claimSetToDelete = _context.ClaimSets.Single(x => x.ClaimSetId == claimSet.Id);
        if (claimSetToDelete.ForApplicationUseOnly || claimSetToDelete.IsEdfiPreset ||
                Constants.SystemReservedClaimSets.Contains(claimSetToDelete.ClaimSetName))
        {
            throw new AdminApiException($"Claim set({claimSetToDelete.ClaimSetName}) is system reserved.Can not be deleted.");
        }

        var resourceClaimsForClaimSetId =
                  _context.ClaimSetResourceClaimActions.Where(x => x.ClaimSet.ClaimSetId == claimSet.Id).ToList();
        foreach (var resourceClaimAction in resourceClaimsForClaimSetId)
        {
            var resourceClaimActionAuthorizationStrategyOverrides = _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.
                Where(x => x.ClaimSetResourceClaimActionId == resourceClaimAction.ClaimSetResourceClaimActionId);

            _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.RemoveRange(resourceClaimActionAuthorizationStrategyOverrides);
        }

        _context.ClaimSetResourceClaimActions.RemoveRange(resourceClaimsForClaimSetId);
        _context.ClaimSets.Remove(claimSetToDelete);
        _context.SaveChanges();
    }

}
