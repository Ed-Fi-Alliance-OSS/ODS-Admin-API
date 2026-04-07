// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using SecurityResourceClaim = EdFi.Security.DataAccess.Models.ResourceClaim;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor
{
    public class GetResourcesByClaimSetIdQuery : IGetResourcesByClaimSetIdQuery
    {
        private readonly ISecurityContext _securityContext;

        public GetResourcesByClaimSetIdQuery(ISecurityContext securityContext)
        {
            _securityContext = securityContext;
        }

        public IList<ResourceClaim> AllResources(int securityContextClaimSetId)
        {
            var parentResources = GetParentResources(securityContextClaimSetId);
                var childResources = GetChildResources(securityContextClaimSetId);
                AddChildResourcesToParents(childResources, parentResources);
            return parentResources;
        }

        public ResourceClaim? SingleResource(int claimSetId, int resourceClaimId)
        {
            var parentResources = AllResources(claimSetId).ToList();
            var parentResourceClaim = parentResources
                .SingleOrDefault(x => x.Id == resourceClaimId);
            var childResources = new List<ResourceClaim>();
            if (parentResourceClaim == null)
            {
                foreach (var resourceClaims in parentResources.Select(x => x.Children)) childResources.AddRange(resourceClaims);
                return childResources.SingleOrDefault(x => x.Id == resourceClaimId);
            }

            return parentResourceClaim;
        }

        internal static void AddChildResourcesToParents(IReadOnlyList<ResourceClaim> childResources, IList<ResourceClaim> parentResources)
        {
            foreach (var childResource in childResources)
            {
                var parentResource = parentResources.SingleOrDefault(x => x.Id == childResource.ParentId);
                if (parentResource != null)
                    parentResource.Children.Add(childResource);
                else
                {
                    parentResources.Add(childResource);
                }
            }
        }

        internal IList<ResourceClaim> GetParentResources(int claimSetId)
        {
            var dbParentResources = _securityContext.ClaimSetResourceClaimActions
                .Include(x => x.ResourceClaim)
                .Include(x => x.ResourceClaim.ParentResourceClaim)
                .Include(x => x.Action)
                .Include(x => x.AuthorizationStrategyOverrides)
                    .ThenInclude(x => x.AuthorizationStrategy)
                .Where(x => x.ClaimSet.ClaimSetId == claimSetId
                            && x.ResourceClaim.ParentResourceClaimId == null).ToList();

            var defaultAuthStrategies = GetDefaultAuthStrategies(dbParentResources.Select(x => x.ResourceClaim).ToList());
            var authStrategyOverrides = GetAuthStrategyOverrides(dbParentResources.ToList());

            var parentResources = dbParentResources.GroupBy(x => x.ResourceClaim).Select(x => new ResourceClaim
            {
                Id = x.Key.ResourceClaimId,
                Name = x.Key.ResourceName,
                Actions = x.Where(x => x.Action != null).Select(x =>
                             new ResourceClaimAction { Name = x.Action.ActionName, Enabled = true}).ToList(),
                IsParent = true,
                DefaultAuthorizationStrategiesForCRUD = defaultAuthStrategies[x.Key.ResourceClaimId],
                AuthorizationStrategyOverridesForCRUD = authStrategyOverrides[x.Key.ResourceClaimId]
            }).ToList();

            parentResources.ForEach(x => x.Children = new List<ResourceClaim>());
            return parentResources;
        }

        public Dictionary<int, List<ClaimSetResourceClaimActionAuthStrategies?>> GetDefaultAuthStrategies(IReadOnlyCollection<SecurityResourceClaim> resourceClaims)
        {
            var resultDictionary = new Dictionary<int, List<ClaimSetResourceClaimActionAuthStrategies?>>();

            var defaultAuthStrategies = _securityContext.ResourceClaimActions
                .Include(x => x.ResourceClaim)
                .Include(x => x.Action)
                .Include(x => x.AuthorizationStrategies)
                    .ThenInclude(x => x.AuthorizationStrategy).ToList();

            var defaultAuthStrategiesForParents = defaultAuthStrategies
                .Where(x => x.ResourceClaim.ParentResourceClaimId == null).ToList();

            var defaultAuthStrategiesForChildren = defaultAuthStrategies
                .Where(x => x.ResourceClaim.ParentResourceClaimId != null).ToList();

            foreach (var resourceClaim in resourceClaims)
            {
                var actions = new List<ClaimSetResourceClaimActionAuthStrategies?>();
                foreach (var action in _securityContext.Actions)
                {
                    if (resourceClaim.ParentResourceClaimId == null)
                    {
                        var defaultStrategies = defaultAuthStrategiesForParents.SingleOrDefault(x =>
                          x.ResourceClaim.ResourceClaimId == resourceClaim.ResourceClaimId &&
                          x.Action.ActionName == action.ActionName)?.AuthorizationStrategies?.Select(x => x.AuthorizationStrategy);

                        if (defaultStrategies != null)
                        {
                            actions.Add(new ClaimSetResourceClaimActionAuthStrategies
                            {
                                ActionId = action.ActionId,
                                ActionName = action.ActionName,
                                AuthorizationStrategies = ToAuthorizationStrategies(defaultStrategies)
                            });
                        }
                    }
                    else
                    {
                        List<AuthorizationStrategy>? childResourceStrategies = null;
                        var defaultStrategies = defaultAuthStrategiesForChildren.SingleOrDefault(x =>
                         x.ResourceClaim.ResourceClaimId == resourceClaim.ResourceClaimId &&
                         x.Action.ActionName == action.ActionName)?.AuthorizationStrategies?.Select(x => x.AuthorizationStrategy);

                        if (defaultStrategies == null)
                        {
                            defaultStrategies = defaultAuthStrategiesForParents.SingleOrDefault(x =>
                                  x.ResourceClaim.ResourceClaimId == resourceClaim.ParentResourceClaimId &&
                                  x.Action.ActionName == action.ActionName)?.AuthorizationStrategies?.Select(x => x.AuthorizationStrategy);
                            childResourceStrategies = AddStrategiesToChildResource(defaultStrategies, true);
                        }
                        else
                        {
                            childResourceStrategies = AddStrategiesToChildResource(defaultStrategies);
                        }
                        if (childResourceStrategies != null)
                        {
                            actions.Add(new ClaimSetResourceClaimActionAuthStrategies
                            {
                                ActionId = action.ActionId,
                                ActionName = action.ActionName,
                                AuthorizationStrategies = childResourceStrategies
                            });
                        }
                    }
                }

                static List<AuthorizationStrategy> AddStrategiesToChildResource(IEnumerable<SecurityAuthorizationStrategy>? authStrategies, bool fromParent = false)
                {
                    var strategies = new List<AuthorizationStrategy>();
                    if (authStrategies != null && authStrategies.Any())
                    {
                        foreach (var authStratregy in authStrategies)
                        {
                            strategies.Add(new AuthorizationStrategy
                            {
                                AuthStrategyId = authStratregy.AuthorizationStrategyId,
                                AuthStrategyName = authStratregy.AuthorizationStrategyName,
                                IsInheritedFromParent = fromParent
                            });
                        }
                    }
                    return strategies;
                }

                resultDictionary[resourceClaim.ResourceClaimId] = actions.Where(x => x != null &&
                x.AuthorizationStrategies != null && x.AuthorizationStrategies.Any()).ToList();
            }

            return resultDictionary;
        }

        private static List<AuthorizationStrategy> ToAuthorizationStrategies(IEnumerable<SecurityAuthorizationStrategy> authStrategies)
        {
            return authStrategies.Select(authStrategy => new AuthorizationStrategy
            {
                AuthStrategyId = authStrategy.AuthorizationStrategyId,
                AuthStrategyName = authStrategy.AuthorizationStrategyName,
                IsInheritedFromParent = false
            }).ToList();
        }

        private Dictionary<int, List<ClaimSetResourceClaimActionAuthStrategies?>> GetAuthStrategyOverrides(List<ClaimSetResourceClaimAction> resourceClaims)
        {
            var resultDictionary = new Dictionary<int, List<ClaimSetResourceClaimActionAuthStrategies?>>();

            foreach (var resourceClaim in resourceClaims)
            {
                ClaimSetResourceClaimActionAuthStrategies? actionDetails = null;

                if (resourceClaim.ResourceClaim.ParentResourceClaim == null)
                {
                    if (resourceClaim.AuthorizationStrategyOverrides is not null
                      && resourceClaim.AuthorizationStrategyOverrides.Count != 0)
                    {
                        actionDetails = new ClaimSetResourceClaimActionAuthStrategies
                        {
                            ActionId = resourceClaim.ActionId,
                            ActionName = resourceClaim.Action.ActionName,
                            AuthorizationStrategies = AddStrategyOverridesToResource(resourceClaim.AuthorizationStrategyOverrides)
                        };
                    }
                }
                else
                {
                    var parentResources = _securityContext.ClaimSetResourceClaimActions
                        .Include(x => x.ResourceClaim)
                        .Include(x => x.ClaimSet)
                        .Include(x => x.Action)
                        .Include(x => x.AuthorizationStrategyOverrides)
                            .ThenInclude(x => x.AuthorizationStrategy).ToList();

                    var parentResourceOverride = parentResources.SingleOrDefault(x => x.ResourceClaim.ResourceClaimId == resourceClaim.ResourceClaim.ParentResourceClaimId
                                                                                               && x.ClaimSet.ClaimSetId == resourceClaim.ClaimSet.ClaimSetId
                                                                                               && x.Action.ActionId == resourceClaim.Action.ActionId);

                    List<AuthorizationStrategy>? childResourceOverrideStrategies = null;

                    if (parentResourceOverride?.AuthorizationStrategyOverrides != null && parentResourceOverride.AuthorizationStrategyOverrides.Count != 0)
                    {
                        childResourceOverrideStrategies = AddStrategyOverridesToResource(parentResourceOverride.AuthorizationStrategyOverrides, true);
                    }

                    if (resourceClaim.AuthorizationStrategyOverrides != null && resourceClaim.AuthorizationStrategyOverrides.Count != 0)
                    {
                        childResourceOverrideStrategies = AddStrategyOverridesToResource(resourceClaim.AuthorizationStrategyOverrides);
                    }

                    if (childResourceOverrideStrategies != null)
                    {
                        actionDetails = new ClaimSetResourceClaimActionAuthStrategies
                        {
                            ActionId = resourceClaim.ActionId,
                            ActionName = resourceClaim.Action.ActionName,
                            AuthorizationStrategies = childResourceOverrideStrategies
                        };
                    }
                }

                static List<AuthorizationStrategy> AddStrategyOverridesToResource(IEnumerable<ClaimSetResourceClaimActionAuthorizationStrategyOverrides>? authStrategies, bool fromParent = false)
                {
                    var strategies = new List<AuthorizationStrategy>();
                    if (authStrategies != null && authStrategies.Any())
                    {
                        foreach (var authStrategy in authStrategies)
                        {
                            strategies.Add(new AuthorizationStrategy
                            {
                                AuthStrategyId = authStrategy.AuthorizationStrategyId,
                                AuthStrategyName = authStrategy.AuthorizationStrategy?.AuthorizationStrategyName,
                                IsInheritedFromParent = fromParent
                            });
                        }
                    }
                    return strategies;
                }

                if (resultDictionary.ContainsKey(resourceClaim.ResourceClaim.ResourceClaimId))
                {
                    if (actionDetails != null)
                        resultDictionary[resourceClaim.ResourceClaim.ResourceClaimId].Add(actionDetails);
                }
                else
                {
                    resultDictionary[resourceClaim.ResourceClaim.ResourceClaimId] = [];
                    if(actionDetails != null)
                    {
                        resultDictionary[resourceClaim.ResourceClaim.ResourceClaimId].Add(actionDetails);
                    }
                }
            }

            return resultDictionary;
        }

        internal IReadOnlyList<ResourceClaim> GetChildResources(int claimSetId)
        {
            var dbChildResources =
                _securityContext.ClaimSetResourceClaimActions
                .Include(x => x.ResourceClaim)
                .Include(x => x.Action)
                .Where(x => x.ClaimSet.ClaimSetId == claimSetId
                            && x.ResourceClaim.ParentResourceClaimId != null).ToList();
            var defaultAuthStrategies = GetDefaultAuthStrategies(dbChildResources.Select(x => x.ResourceClaim).ToList());
            var authStrategyOverrides = GetAuthStrategyOverrides([.. dbChildResources]);

            var childResources = dbChildResources.GroupBy(x => x.ResourceClaim)
                .Select(x => new ResourceClaim
                {
                    Id = x.Key.ResourceClaimId,
                    ParentId = x.Key.ParentResourceClaimId ?? 0,
                    Name = x.Key.ResourceName,
                    Actions = x.Where(x => x.Action != null).Select(x =>
                             new ResourceClaimAction { Name = x.Action.ActionName, Enabled = true }).ToList(),
                    IsParent = false,
                    DefaultAuthorizationStrategiesForCRUD = defaultAuthStrategies[x.Key.ResourceClaimId],
                    AuthorizationStrategyOverridesForCRUD = authStrategyOverrides[x.Key.ResourceClaimId]
                })
                .ToList();
            return childResources;
        }
    }

    public interface IGetResourcesByClaimSetIdQuery
    {
        IList<ResourceClaim> AllResources(int securityContextClaimSetId);
        ResourceClaim? SingleResource(int claimSetId, int resourceClaimId);
    }
}
