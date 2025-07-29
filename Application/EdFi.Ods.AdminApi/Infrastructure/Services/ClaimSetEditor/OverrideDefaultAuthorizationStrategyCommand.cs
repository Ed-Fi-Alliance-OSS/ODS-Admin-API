// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public class OverrideDefaultAuthorizationStrategyCommand
{
    private readonly ISecurityContext _context;
    public OverrideDefaultAuthorizationStrategyCommand(ISecurityContext context)
    {
        _context = context;
    }

    public void Execute(IOverrideDefaultAuthorizationStrategyModel model)
    {
        var claimSetResourceClaimsToEdit = GetClaimSetResourceClaimsToEdit(model.ClaimSetId, model.ResourceClaimId);
        var parentResourceClaims = GetParentResourceClaims(model.ClaimSetId, model.ResourceClaimId);

        var authorizationStrategiesDictionary = GetAuthorizationStrategiesAsDictionary();

        claimSetResourceClaimsToEdit = RemoveOverrides(model, claimSetResourceClaimsToEdit);

        AddOverrides(model, claimSetResourceClaimsToEdit, authorizationStrategiesDictionary, parentResourceClaims);

        _context.SaveChanges();
    }


    public void ExecuteOnSpecificAction(OverrideAuthStrategyOnClaimSetModel model)
    {
        var authorizationStrategiesDictionary = GetAuthorizationStrategiesAsDictionary();
        var claimSetResourceClaimsToEdit = GetClaimSetResourceClaimsToEdit(model.ClaimSetId, model.ResourceClaimId);

        var claimSetResourceClaimAction = claimSetResourceClaimsToEdit.Find(rc => rc.ResourceClaimId == model.ResourceClaimId &&
            rc.Action.ActionName.Equals(model.ActionName, StringComparison.CurrentCultureIgnoreCase));

        if (claimSetResourceClaimAction != null)
        {
            var resourceClaimActionDefaultAuthorizationStrategy = _context.ResourceClaimActionAuthorizationStrategies.AsEnumerable().FirstOrDefault(p => p.ResourceClaimAction.ResourceClaimId == model.ResourceClaimId
                && p.ResourceClaimAction.Action.ActionName.Equals(model.ActionName, StringComparison.CurrentCultureIgnoreCase));

            if (resourceClaimActionDefaultAuthorizationStrategy == null && claimSetResourceClaimAction.ResourceClaim.ParentResourceClaim != null)
            {
                var parentResourceClaimId = claimSetResourceClaimAction.ResourceClaim.ParentResourceClaim.ResourceClaimId;
                var parentResourceClaimDefaultAuthStrategy = _context.ResourceClaimActionAuthorizationStrategies.FirstOrDefault(p =>
                p.ResourceClaimAction.ResourceClaimId == parentResourceClaimId && p.ResourceClaimAction.Action.ActionName.ToLower() == model.ActionName!.ToLower());
                resourceClaimActionDefaultAuthorizationStrategy = parentResourceClaimDefaultAuthStrategy;
            }

            if (!claimSetResourceClaimAction!.AuthorizationStrategyOverrides.Exists(rc => rc.ClaimSetResourceClaimAction.Action.ActionName.Equals(model.ActionName, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (model.AuthStrategyIds != null)
                    foreach (var id in model.AuthStrategyIds)
                    {
                        if (resourceClaimActionDefaultAuthorizationStrategy != null &&
                            resourceClaimActionDefaultAuthorizationStrategy.AuthorizationStrategyId != id)
                        {
                            claimSetResourceClaimAction!.AuthorizationStrategyOverrides.Add(new ClaimSetResourceClaimActionAuthorizationStrategyOverrides()
                            {
                                AuthorizationStrategy = authorizationStrategiesDictionary[id]
                            });
                        }

                    }
            }
            else
            {
                if (model.AuthStrategyIds != null)
                {
                    RemoveClaimSetResourceClaimActionAuthorizationStrategyOverrides(claimSetResourceClaimAction);

                    var overrideAuthStrategies = new List<ClaimSetResourceClaimActionAuthorizationStrategyOverrides>();
                    foreach (var id in model.AuthStrategyIds)
                    {
                        if (resourceClaimActionDefaultAuthorizationStrategy != null &&
                            resourceClaimActionDefaultAuthorizationStrategy.AuthorizationStrategyId != id)
                        {
                            overrideAuthStrategies.Add(new ClaimSetResourceClaimActionAuthorizationStrategyOverrides()
                            {
                                AuthorizationStrategy = authorizationStrategiesDictionary[id]
                            });
                        }
                    }
                    claimSetResourceClaimAction.AuthorizationStrategyOverrides = overrideAuthStrategies;
                }
            }
            _context.SaveChanges();
        }
        else
        {
            var validationErrors = new List<ValidationFailure>
            {
                new() {
                    PropertyName = "Action",
                    ErrorMessage = $"{model.ActionName} action is not enabled for the resource claim with id {model.ResourceClaimId}."
                }
            };
            throw new ValidationException(validationErrors);
        }
    }

    public void ResetAuthorizationStrategyOverrides(OverrideAuthStrategyOnClaimSetModel model)
    {
        var claimSetResourceClaimActionsToEdit = GetClaimSetResourceClaimsToEdit(model.ClaimSetId, model.ResourceClaimId);

        foreach (var resourceClaimAction in claimSetResourceClaimActionsToEdit)
        {
            RemoveClaimSetResourceClaimActionAuthorizationStrategyOverrides(resourceClaimAction);
        }
        _context.SaveChanges();
    }

    private Dictionary<int, EdFi.Security.DataAccess.Models.AuthorizationStrategy> GetAuthorizationStrategiesAsDictionary()
    {
        var authorizationStrategiesDictionary =
            new Dictionary<int, EdFi.Security.DataAccess.Models.AuthorizationStrategy>();

        foreach (var authStrategy in _context.AuthorizationStrategies.ToList())
        {
            authorizationStrategiesDictionary[authStrategy.AuthorizationStrategyId] = authStrategy;
        }

        return authorizationStrategiesDictionary;
    }

    private List<ClaimSetResourceClaimAction> GetParentResourceClaims(int claimSetId, int resourceClaimId)
    {
        var parentResourceClaims = new List<ClaimSetResourceClaimAction>();
        var parentResourceClaimId = _context.ResourceClaims
                    .Single(x => x.ResourceClaimId == resourceClaimId).ParentResourceClaimId;
        if (parentResourceClaimId != null)
        {
            parentResourceClaims = [.. _context.ClaimSetResourceClaimActions
                .Include(x => x.ResourceClaim)
                .Include(x => x.Action)
                .Include(x => x.ClaimSet)
                .Include(x => x.AuthorizationStrategyOverrides)
                .ThenInclude(x => x.AuthorizationStrategy)
                .Where(
                    x => x.ResourceClaim.ResourceClaimId == parentResourceClaimId &&
                         x.ClaimSet.ClaimSetId == claimSetId)];
        }

        return parentResourceClaims;
    }

    private List<ClaimSetResourceClaimAction> GetClaimSetResourceClaimsToEdit(int claimSetId, int resourceClaimId)
    {
        return [.. _context.ClaimSetResourceClaimActions
                     .Include(x => x.ResourceClaim)
                     .Include(x => x.Action)
                     .Include(x => x.ClaimSet)
                     .Include(x => x.AuthorizationStrategyOverrides)
                        .ThenInclude(x => x.AuthorizationStrategy)
                     .Where(
                         x => x.ResourceClaim.ResourceClaimId == resourceClaimId &&
                              x.ClaimSet.ClaimSetId == claimSetId)];
    }

    private List<ClaimSetResourceClaimAction> RemoveOverrides(
       IOverrideDefaultAuthorizationStrategyModel model,
       IEnumerable<ClaimSetResourceClaimAction> resourceClaimsToEdit)
    {
        var claimSetResourceClaims = resourceClaimsToEdit.ToList();

        foreach (var claimSetResourceClaim in claimSetResourceClaims)
        {
            var actionAuthStrategiesToOverride = model.ClaimSetResourceClaimActionAuthStrategyOverrides?.Where(x =>
                    x != null && x.ActionName != null && x.ActionName.Equals(claimSetResourceClaim.Action.ActionName, StringComparison.InvariantCultureIgnoreCase));

            if (actionAuthStrategiesToOverride != null && actionAuthStrategiesToOverride.Any())
            {
                claimSetResourceClaim.AuthorizationStrategyOverrides = null;
            }

            RemoveClaimSetResourceClaimActionAuthorizationStrategyOverrides(claimSetResourceClaim);
        }

        return claimSetResourceClaims;
    }

    private void RemoveClaimSetResourceClaimActionAuthorizationStrategyOverrides(ClaimSetResourceClaimAction claimSetResourceClaimAction)
    {
        var existingAuthOverrides =
            _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.Where(
                x => x.ClaimSetResourceClaimActionId ==
                     claimSetResourceClaimAction.ClaimSetResourceClaimActionId);

        if (existingAuthOverrides.Any())
        {
            _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.RemoveRange(
                existingAuthOverrides);
        }
    }

    private static void AddOverrides(IOverrideDefaultAuthorizationStrategyModel model,
        IEnumerable<ClaimSetResourceClaimAction> resourceClaimsToEdit,
        Dictionary<int, EdFi.Security.DataAccess.Models.AuthorizationStrategy> authorizationStrategiesDictionary,
        List<ClaimSetResourceClaimAction> parentResourceClaims)
    {
        var claimSetResourceClaims = resourceClaimsToEdit.ToList();

        foreach (var claimSetResourceClaim in claimSetResourceClaims)
        {
            var actionAuthStrategiesToOverride = model.ClaimSetResourceClaimActionAuthStrategyOverrides?.Find(x =>
               x != null && x.ActionName != null && x.ActionName.Equals(claimSetResourceClaim.Action.ActionName, StringComparison.InvariantCultureIgnoreCase));

            List<ClaimSetResourceClaimActionAuthorizationStrategyOverrides>? authStrategyOverrides = null;

            if (actionAuthStrategiesToOverride != null)
            {
                authStrategyOverrides = [];
                if (actionAuthStrategiesToOverride.AuthorizationStrategies != null)
                    foreach (var actionAuthOverride in actionAuthStrategiesToOverride.AuthorizationStrategies)
                    {
                        authStrategyOverrides.Add(new()
                        {
                            AuthorizationStrategy = authorizationStrategiesDictionary[actionAuthOverride.AuthStrategyId]
                        });
                    }

                if (parentResourceClaims.Count != 0 && parentResourceClaims.SingleOrDefault(
                    x => x.Action.ActionId == claimSetResourceClaim.ActionId
                    && x.AuthorizationStrategyOverrides != null &&
                        x.AuthorizationStrategyOverrides.Exists(a =>
                            authStrategyOverrides.Select(c => c.AuthorizationStrategyId).
                                                                Contains(a.AuthorizationStrategyId))) == null
                    || parentResourceClaims.Count == 0)
                {
                    claimSetResourceClaim.AuthorizationStrategyOverrides = authStrategyOverrides;
                }
            }
        }
    }
}


public interface IOverrideDefaultAuthorizationStrategyModel
{
    int ClaimSetId { get; }
    int ResourceClaimId { get; }
    List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides { get; }
}

public class OverrideAuthStrategyOnClaimSetModel
{
    [SwaggerExclude]
    public int ClaimSetId { get; set; }
    [SwaggerExclude]
    public int ResourceClaimId { get; set; }
    public string? ActionName { get; set; }
    [SwaggerExclude]
    public List<int>? AuthStrategyIds { get; set; }
}

