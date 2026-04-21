// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using CommonAuthorizationStrategy = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.AuthorizationStrategy;
using CommonClaimSetResourceClaimActionAuthStrategies = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies;
using CommonResourceClaim = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaim;
using CommonResourceClaimAction = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaimAction;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public interface IAuthStrategyResolver
{
    IEnumerable<ResourceClaim> ResolveAuthStrategies(IEnumerable<ResourceClaim> resourceClaims);
}

public class AuthStrategyResolver(ISecurityContext securityContext)
    : AuthStrategyResolverBase(securityContext), IAuthStrategyResolver
{
    public IEnumerable<ResourceClaim> ResolveAuthStrategies(IEnumerable<ResourceClaim> resourceClaims)
    {
        return ResolveAuthStrategiesCore(resourceClaims.Select(MapToCommon))
            .Select(MapFromCommon)
            .ToList();
    }

    private static CommonResourceClaim MapToCommon(ResourceClaim resourceClaim)
    {
        return new CommonResourceClaim
        {
            Id = resourceClaim.Id,
            ParentId = resourceClaim.ParentId,
            ParentName = resourceClaim.ParentName,
            Name = resourceClaim.Name,
            IsParent = resourceClaim.IsParent,
            Actions = resourceClaim.Actions?.Select(MapToCommon).ToList(),
            DefaultAuthorizationStrategiesForCRUD = resourceClaim.DefaultAuthorizationStrategiesForCRUD
                .Select(x => x == null ? null : MapToCommon(x))
                .ToList(),
            AuthorizationStrategyOverridesForCRUD = resourceClaim.AuthorizationStrategyOverridesForCRUD
                .Select(x => x == null ? null : MapToCommon(x))
                .ToList(),
            Children = resourceClaim.Children.Select(MapToCommon).ToList()
        };
    }

    private static CommonResourceClaimAction MapToCommon(ResourceClaimAction action)
    {
        return new CommonResourceClaimAction
        {
            Name = action.Name,
            Enabled = action.Enabled
        };
    }

    private static CommonClaimSetResourceClaimActionAuthStrategies MapToCommon(ClaimSetResourceClaimActionAuthStrategies authorizationStrategy)
    {
        return new CommonClaimSetResourceClaimActionAuthStrategies
        {
            ActionId = authorizationStrategy.ActionId,
            ActionName = authorizationStrategy.ActionName,
            AuthorizationStrategies = authorizationStrategy.AuthorizationStrategies?.Select(MapToCommon).ToList()
        };
    }

    private static CommonAuthorizationStrategy MapToCommon(AuthorizationStrategy strategy)
    {
        return new CommonAuthorizationStrategy
        {
            AuthStrategyId = strategy.AuthStrategyId,
            AuthStrategyName = strategy.AuthStrategyName,
            IsInheritedFromParent = strategy.IsInheritedFromParent
        };
    }

    private static ResourceClaim MapFromCommon(CommonResourceClaim resourceClaim)
    {
        return new ResourceClaim
        {
            Id = resourceClaim.Id,
            ParentId = resourceClaim.ParentId,
            ParentName = resourceClaim.ParentName,
            Name = resourceClaim.Name,
            IsParent = resourceClaim.IsParent,
            Actions = resourceClaim.Actions?.Select(MapFromCommon).ToList(),
            DefaultAuthorizationStrategiesForCRUD = resourceClaim.DefaultAuthorizationStrategiesForCRUD
                .Select(x => x == null ? null : MapFromCommon(x))
                .ToList(),
            AuthorizationStrategyOverridesForCRUD = resourceClaim.AuthorizationStrategyOverridesForCRUD
                .Select(x => x == null ? null : MapFromCommon(x))
                .ToList(),
            Children = resourceClaim.Children.Select(MapFromCommon).ToList()
        };
    }

    private static ResourceClaimAction MapFromCommon(CommonResourceClaimAction action)
    {
        return new ResourceClaimAction
        {
            Name = action.Name,
            Enabled = action.Enabled
        };
    }

    private static ClaimSetResourceClaimActionAuthStrategies MapFromCommon(CommonClaimSetResourceClaimActionAuthStrategies authorizationStrategy)
    {
        return new ClaimSetResourceClaimActionAuthStrategies
        {
            ActionId = authorizationStrategy.ActionId,
            ActionName = authorizationStrategy.ActionName,
            AuthorizationStrategies = authorizationStrategy.AuthorizationStrategies?.Select(MapFromCommon).ToList()
        };
    }

    private static AuthorizationStrategy MapFromCommon(CommonAuthorizationStrategy strategy)
    {
        return new AuthorizationStrategy
        {
            AuthStrategyId = strategy.AuthStrategyId,
            AuthStrategyName = strategy.AuthStrategyName,
            IsInheritedFromParent = strategy.IsInheritedFromParent
        };
    }
}

