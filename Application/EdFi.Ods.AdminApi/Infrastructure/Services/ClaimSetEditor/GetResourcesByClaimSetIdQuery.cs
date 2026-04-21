// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using CommonAuthorizationStrategy = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.AuthorizationStrategy;
using CommonClaimSetResourceClaimActionAuthStrategies = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies;
using CommonResourceClaim = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaim;
using CommonResourceClaimAction = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaimAction;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor
{
    public class GetResourcesByClaimSetIdQuery(ISecurityContext securityContext)
        : GetResourcesByClaimSetIdQueryBase(securityContext), IGetResourcesByClaimSetIdQuery
    {
        public IList<ResourceClaim> AllResources(int securityContextClaimSetId)
        {
            return AllResourcesCore(securityContextClaimSetId)
                .Select(Map)
                .ToList();
        }

        public ResourceClaim? SingleResource(int claimSetId, int resourceClaimId)
        {
            var resourceClaim = SingleResourceCore(claimSetId, resourceClaimId);
            return resourceClaim == null ? null : Map(resourceClaim);
        }

        private static ResourceClaim Map(CommonResourceClaim resourceClaim)
        {
            return new ResourceClaim
            {
                Id = resourceClaim.Id,
                ParentId = resourceClaim.ParentId,
                ParentName = resourceClaim.ParentName,
                Name = resourceClaim.Name,
                IsParent = resourceClaim.IsParent,
                Actions = resourceClaim.Actions?.Select(Map).ToList(),
                DefaultAuthorizationStrategiesForCRUD = resourceClaim.DefaultAuthorizationStrategiesForCRUD
                    .Select(x => x == null ? null : Map(x))
                    .ToList(),
                AuthorizationStrategyOverridesForCRUD = resourceClaim.AuthorizationStrategyOverridesForCRUD
                    .Select(x => x == null ? null : Map(x))
                    .ToList(),
                Children = resourceClaim.Children.Select(Map).ToList()
            };
        }

        private static ResourceClaimAction Map(CommonResourceClaimAction action)
        {
            return new ResourceClaimAction
            {
                Name = action.Name,
                Enabled = action.Enabled
            };
        }

        private static ClaimSetResourceClaimActionAuthStrategies Map(CommonClaimSetResourceClaimActionAuthStrategies authorizationStrategy)
        {
            return new ClaimSetResourceClaimActionAuthStrategies
            {
                ActionId = authorizationStrategy.ActionId,
                ActionName = authorizationStrategy.ActionName,
                AuthorizationStrategies = authorizationStrategy.AuthorizationStrategies?.Select(Map).ToList()
            };
        }

        private static AuthorizationStrategy Map(CommonAuthorizationStrategy strategy)
        {
            return new AuthorizationStrategy
            {
                AuthStrategyId = strategy.AuthStrategyId,
                AuthStrategyName = strategy.AuthStrategyName,
                IsInheritedFromParent = strategy.IsInheritedFromParent
            };
        }
    }

    public interface IGetResourcesByClaimSetIdQuery
    {
        IList<ResourceClaim> AllResources(int securityContextClaimSetId);
        ResourceClaim? SingleResource(int claimSetId, int resourceClaimId);
    }
}
