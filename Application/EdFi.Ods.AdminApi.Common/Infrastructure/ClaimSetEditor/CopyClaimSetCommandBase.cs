// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

public interface ICopyClaimSetModelCommon
{
    string? Name { get; }
    int OriginalId { get; }
}

public abstract class CopyClaimSetCommandBase(ISecurityContext context)
{
    private readonly ISecurityContext _context = context;

    protected int ExecuteCore(ICopyClaimSetModelCommon claimSet)
    {
        var newClaimSet = new EdFi.Security.DataAccess.Models.ClaimSet
        {
            ClaimSetName = claimSet.Name,
            IsEdfiPreset = false,
            ForApplicationUseOnly = false
        };

        var originalResourceClaims = _context.ClaimSetResourceClaimActions
            .Where(x => x.ClaimSet.ClaimSetId == claimSet.OriginalId)
            .Include(x => x.ResourceClaim)
            .Include(x => x.Action)
            .Include(x => x.AuthorizationStrategyOverrides)
            .ThenInclude(x => x.AuthorizationStrategy)
            .ToList();

        _context.ClaimSets.Add(newClaimSet);

        foreach (var resourceClaim in originalResourceClaims.ToList())
        {
            List<EdFi.Security.DataAccess.Models.ClaimSetResourceClaimActionAuthorizationStrategyOverrides>? authStrategyOverrides = null;
            if (resourceClaim.AuthorizationStrategyOverrides != null && resourceClaim.AuthorizationStrategyOverrides.Any())
            {
                authStrategyOverrides = [];
                foreach (var authStrategyOverride in resourceClaim.AuthorizationStrategyOverrides)
                {
                    authStrategyOverrides.Add(new EdFi.Security.DataAccess.Models.ClaimSetResourceClaimActionAuthorizationStrategyOverrides
                    {
                        AuthorizationStrategy = authStrategyOverride.AuthorizationStrategy
                    });
                }
            }

            var copyResourceClaim = new EdFi.Security.DataAccess.Models.ClaimSetResourceClaimAction
            {
                ClaimSet = newClaimSet,
                Action = resourceClaim.Action,
                AuthorizationStrategyOverrides = authStrategyOverrides,
                ResourceClaim = resourceClaim.ResourceClaim
            };
            _context.ClaimSetResourceClaimActions.Add(copyResourceClaim);
        }

        _context.SaveChanges();

        return newClaimSet.ClaimSetId;
    }
}
