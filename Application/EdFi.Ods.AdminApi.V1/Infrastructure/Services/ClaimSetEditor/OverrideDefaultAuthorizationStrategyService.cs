// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class OverrideDefaultAuthorizationStrategyService
{
    private readonly ISecurityContext _context;

    public OverrideDefaultAuthorizationStrategyService(ISecurityContext context)
    {
        _context = context;
    }

    public void Execute(IOverrideDefaultAuthorizationStrategyModel model)
    {
        var claimSetResourceClaimsToEdit = _context.ClaimSetResourceClaimActions
            .Include(x => x.ResourceClaim)
            .Include(x => x.Action)
            .Include(x => x.ClaimSet)
            .Include(x => x.AuthorizationStrategyOverrides!).ThenInclude(x => x.AuthorizationStrategy)
            .Where(
                x => x.ResourceClaim.ResourceClaimId == model.ResourceClaimId &&
                     x.ClaimSet.ClaimSetId == model.ClaimSetId)
            .ToList();

        var parentResourceClaimId = _context.ResourceClaims
            .Single(x => x.ResourceClaimId == model.ResourceClaimId).ParentResourceClaimId;

        var parentResourceClaims = new List<ClaimSetResourceClaimAction>();

        if (parentResourceClaimId != null)
        {
            parentResourceClaims = _context.ClaimSetResourceClaimActions
                .Include(x => x.ResourceClaim)
                .Include(x => x.Action)
                .Include(x => x.ClaimSet)
                .Include(x => x.AuthorizationStrategyOverrides!).ThenInclude(x => x.AuthorizationStrategy)
                .Where(
                    x => x.ResourceClaim.ResourceClaimId == parentResourceClaimId &&
                         x.ClaimSet.ClaimSetId == model.ClaimSetId)
                .ToList();
        }

        var authorizationStrategiesDictionary =
            new Dictionary<int, Security.DataAccess.Models.AuthorizationStrategy>();

        foreach (var authStrategy in _context.AuthorizationStrategies.ToList())
        {
            authorizationStrategiesDictionary[authStrategy.AuthorizationStrategyId] = authStrategy;
        }

        claimSetResourceClaimsToEdit = RemoveOverrides(model, claimSetResourceClaimsToEdit);

        AddOverrides(model, claimSetResourceClaimsToEdit, authorizationStrategiesDictionary, parentResourceClaims);

        _context.SaveChanges();
    }

    private List<ClaimSetResourceClaimAction> RemoveOverrides(
        IOverrideDefaultAuthorizationStrategyModel model,
        IEnumerable<ClaimSetResourceClaimAction> resourceClaimsToEdit)
    {
        var claimSetResourceClaims = resourceClaimsToEdit.ToList();

        foreach (var claimSetResourceClaim in claimSetResourceClaims)
        {
            if (claimSetResourceClaim.Action.ActionName == Action.Create.Value && model.AuthorizationStrategyForCreate != null &&
                model.AuthorizationStrategyForCreate.Length == 0)
            {
                claimSetResourceClaim.AuthorizationStrategyOverrides = null;
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Read.Value && model.AuthorizationStrategyForRead != null &&
                     model.AuthorizationStrategyForRead.Length == 0)
            {
                claimSetResourceClaim.AuthorizationStrategyOverrides = null;
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Update.Value && model.AuthorizationStrategyForUpdate != null &&
                     model.AuthorizationStrategyForUpdate.Length == 0)
            {
                claimSetResourceClaim.AuthorizationStrategyOverrides = null;
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Delete.Value && model.AuthorizationStrategyForDelete != null &&
                     model.AuthorizationStrategyForDelete.Length == 0)
            {
                claimSetResourceClaim.AuthorizationStrategyOverrides = null;
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.ReadChanges.Value && model.AuthorizationStrategyForReadChanges != null &&
                     model.AuthorizationStrategyForReadChanges.Length == 0)
            {
                claimSetResourceClaim.AuthorizationStrategyOverrides = null;
            }

            RemoveClaimSetResourceClaimActionAuthorizationStrategyOverrides(claimSetResourceClaim);
        }

        void RemoveClaimSetResourceClaimActionAuthorizationStrategyOverrides(ClaimSetResourceClaimAction claimSetResourceClaimAction)
        {
            var existingAuthOverride =
                _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.FirstOrDefault(
                    x => x.ClaimSetResourceClaimActionId ==
                         claimSetResourceClaimAction.ClaimSetResourceClaimActionId);

            if (existingAuthOverride != null)
            {
                _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.Remove(
                    existingAuthOverride);
            }
        }

        return claimSetResourceClaims;
    }

    private static void AddOverrides(IOverrideDefaultAuthorizationStrategyModel model,
        IEnumerable<ClaimSetResourceClaimAction> resourceClaimsToEdit,
        Dictionary<int, Security.DataAccess.Models.AuthorizationStrategy> authorizationStrategiesDictionary,
        List<ClaimSetResourceClaimAction> parentResourceClaims)
    {
        var claimSetResourceClaims = resourceClaimsToEdit.ToList();

        foreach (var claimSetResourceClaim in claimSetResourceClaims)
        {

            if (claimSetResourceClaim.Action.ActionName == Action.Create.Value && model.AuthorizationStrategyForCreate != null &&
                model.AuthorizationStrategyForCreate.Length != 0)
            {
                SetAuthorizationStrategyOverrides(
                    claimSetResourceClaim, parentResourceClaims, model.AuthorizationStrategyForCreate,
                    authorizationStrategiesDictionary, Action.Create.Value);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Read.Value && model.AuthorizationStrategyForRead != null &&
                     model.AuthorizationStrategyForRead.Length != 0)
            {
                SetAuthorizationStrategyOverrides(
                    claimSetResourceClaim, parentResourceClaims, model.AuthorizationStrategyForRead,
                    authorizationStrategiesDictionary, Action.Read.Value);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Update.Value && model.AuthorizationStrategyForUpdate != null &&
                     model.AuthorizationStrategyForUpdate.Length != 0)
            {
                SetAuthorizationStrategyOverrides(
                    claimSetResourceClaim, parentResourceClaims, model.AuthorizationStrategyForUpdate,
                    authorizationStrategiesDictionary, Action.Update.Value);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Delete.Value && model.AuthorizationStrategyForDelete != null &&
                     model.AuthorizationStrategyForDelete.Length != 0)
            {
                SetAuthorizationStrategyOverrides(
                    claimSetResourceClaim, parentResourceClaims, model.AuthorizationStrategyForDelete,
                    authorizationStrategiesDictionary, Action.Delete.Value);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.ReadChanges.Value && model.AuthorizationStrategyForReadChanges != null &&
                     model.AuthorizationStrategyForReadChanges.Length != 0)
            {
                SetAuthorizationStrategyOverrides(
                    claimSetResourceClaim, parentResourceClaims, model.AuthorizationStrategyForReadChanges,
                    authorizationStrategiesDictionary, Action.ReadChanges.Value);
            }
        }
    }

    private static void SetAuthorizationStrategyOverrides(
       ClaimSetResourceClaimAction claimSetResourceClaimAction,
       List<ClaimSetResourceClaimAction> parentResourceClaims, int[] authorizationStrategyValues,
       Dictionary<int, Security.DataAccess.Models.AuthorizationStrategy> authorizationStrategiesDictionary, string actionName)
    {
        foreach (var authStrategyId in authorizationStrategyValues.Where(x => x != 0))
        {
            var authStrategyOverride = new ClaimSetResourceClaimActionAuthorizationStrategyOverrides()
            {
                AuthorizationStrategy = authorizationStrategiesDictionary[authStrategyId],
                ClaimSetResourceClaimAction = claimSetResourceClaimAction
            };

            if (claimSetResourceClaimAction.AuthorizationStrategyOverrides == null)
            {
                claimSetResourceClaimAction.AuthorizationStrategyOverrides = new List<ClaimSetResourceClaimActionAuthorizationStrategyOverrides>();
            }

            if (parentResourceClaims.Any() && parentResourceClaims.SingleOrDefault(
                    x =>
                        x.Action.ActionName == actionName && x.AuthorizationStrategyOverrides != null &&
                        x.AuthorizationStrategyOverrides.Any() &&
                        x.AuthorizationStrategyOverrides.SingleOrDefault()?.AuthorizationStrategyId ==
                        authStrategyId) == null)
            {
                claimSetResourceClaimAction.AuthorizationStrategyOverrides.Add(authStrategyOverride);
            }
            else if (!parentResourceClaims.Any())
            {
                claimSetResourceClaimAction.AuthorizationStrategyOverrides.Add(authStrategyOverride);
            }
        }
    }
}
