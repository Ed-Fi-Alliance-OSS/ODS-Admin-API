// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.Applications;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.V3.Features.ClaimSets;

/// <summary>
/// The only translation seam between the internal, nested ClaimSetEditor resource-claim tree
/// (<see cref="ResourceClaim"/>, used by GetResourcesByClaimSetIdQuery, AuthStrategyResolver,
/// AddOrEditResourcesOnClaimSetCommand, and the per-node editing endpoints EditAuthStrategy /
/// EditResourceClaimActions / DeleteResourceClaim) and the public, flat v3
/// <see cref="ClaimSetResourceClaimModel"/> shape (claimName/parentClaimName based, per the
/// Claim Set Export/Import API Design doc). This is "Approach A": the internal pipeline is left
/// nested/tree-shaped on purpose (it's shared with unrelated editing endpoints), and all
/// flattening/unflattening happens only here.
/// </summary>
public static class ClaimSetMapper
{
    public static ClaimSetModel ToModel(ClaimSet source)
    {
        return new ClaimSetModel
        {
            Id = source.Id,
            Name = source.Name,
            IsSystemReserved = !source.IsEditable
        };
    }

    public static ClaimSetDetailsModel ToDetailsModel(ClaimSet source)
    {
        var model = ToModel(source);

        return new ClaimSetDetailsModel
        {
            Id = model.Id,
            Name = model.Name,
            IsSystemReserved = model.IsSystemReserved,
            Applications = model.Applications
        };
    }

    public static List<ClaimSetModel> ToModelList(IEnumerable<ClaimSet> source)
    {
        return source.Select(ToModel).ToList();
    }

    public static SimpleApplicationModel ToSimpleApplicationModel(Application source)
    {
        return new SimpleApplicationModel
        {
            ApplicationName = source.Name
        };
    }

    public static List<SimpleApplicationModel> ToSimpleApplicationModelList(IEnumerable<Application> source)
    {
        return source.Select(ToSimpleApplicationModel).ToList();
    }

    /// <summary>
    /// Flattens the internal nested resource-claim tree into the public flat list, recording
    /// each node's parent's ClaimName as its parentClaimName (null for roots).
    /// </summary>
    public static List<ClaimSetResourceClaimModel> ToClaimSetResourceClaimModelList(IEnumerable<ResourceClaim> source)
    {
        var flatList = new List<ClaimSetResourceClaimModel>();
        foreach (var resourceClaim in source)
        {
            FlattenResourceClaim(resourceClaim, null, flatList);
        }
        return flatList;
    }

    private static void FlattenResourceClaim(ResourceClaim source, string? parentClaimName, List<ClaimSetResourceClaimModel> flatList)
    {
        flatList.Add(new ClaimSetResourceClaimModel
        {
            Name = source.Name,
            ClaimName = source.ClaimName,
            ParentClaimName = parentClaimName,
            Actions = source.Actions,
            DefaultAuthorizationStrategies = source.DefaultAuthorizationStrategiesForCRUD,
            AuthorizationStrategyOverrides = source.AuthorizationStrategyOverridesForCRUD
        });

        foreach (var child in source.Children)
        {
            FlattenResourceClaim(child, source.ClaimName, flatList);
        }
    }

    /// <summary>
    /// Maps one inbound flat DTO entry to an internal ResourceClaim. Children is always left
    /// empty: AddOrEditResourcesOnClaimSetCommand.Execute already flattens
    /// (resources.SelectMany(x => x.Children)) and matches purely by Name, so a flat 1:1
    /// mapping is sufficient and no tree reconstruction is required.
    /// </summary>
    public static ResourceClaim ToResourceClaim(ClaimSetResourceClaimModel source)
    {
        return new ResourceClaim
        {
            Name = source.Name,
            ClaimName = source.ClaimName,
            ParentClaimName = source.ParentClaimName,
            Actions = source.Actions,
            DefaultAuthorizationStrategiesForCRUD = source.DefaultAuthorizationStrategies,
            AuthorizationStrategyOverridesForCRUD = source.AuthorizationStrategyOverrides,
            Children = new List<ResourceClaim>()
        };
    }

    public static List<ResourceClaim> ToResourceClaimList(IEnumerable<ClaimSetResourceClaimModel> source)
    {
        return source.Select(ToResourceClaim).ToList();
    }

    public static EditResourceOnClaimSetModel ToEditResourceOnClaimSetModel(IResourceClaimOnClaimSetRequest source)
    {
        return new EditResourceOnClaimSetModel
        {
            ClaimSetId = source.ClaimSetId,
            ResourceClaim = new ResourceClaim
            {
                Id = source.ResourceClaimId,
                Actions = source.ResourceClaimActions
            }
        };
    }

    public static OverrideAuthStrategyOnClaimSetModel ToOverrideAuthStrategyOnClaimSetModel(
        ResourceClaims.EditAuthStrategy.OverrideAuthStategyOnClaimSetRequest source,
        List<int> authStrategyIds)
    {
        return new OverrideAuthStrategyOnClaimSetModel
        {
            ClaimSetId = source.ClaimSetId,
            ResourceClaimId = source.ResourceClaimId,
            ActionName = source.ActionName,
            AuthStrategyIds = authStrategyIds
        };
    }

    public static AuthorizationStrategy ToAuthorizationStrategy(SecurityAuthorizationStrategy source, bool isInheritedFromParent = false)
    {
        return new AuthorizationStrategy
        {
            AuthStrategyId = source.AuthorizationStrategyId,
            AuthStrategyName = source.AuthorizationStrategyName,
            IsInheritedFromParent = isInheritedFromParent
        };
    }

    public static List<AuthorizationStrategy> ToAuthorizationStrategyList(IEnumerable<SecurityAuthorizationStrategy> source, bool isInheritedFromParent = false)
    {
        return source.Select(item => ToAuthorizationStrategy(item, isInheritedFromParent)).ToList();
    }
}
