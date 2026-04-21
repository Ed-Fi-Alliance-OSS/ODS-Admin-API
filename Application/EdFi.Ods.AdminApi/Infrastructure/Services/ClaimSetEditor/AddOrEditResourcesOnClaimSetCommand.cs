// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using AuthorizationStrategy = EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public class AddOrEditResourcesOnClaimSetCommand(
    EditResourceOnClaimSetCommand editResourceOnClaimSetCommand,
    IGetResourceClaimsQuery getResourceClaimsQuery,
    OverrideDefaultAuthorizationStrategyCommand overrideDefaultAuthorizationStrategyCommand)
{
    public void Execute(int claimSetId, List<ResourceClaim> resources)
    {
        var commonResources = resources.Select(MapToCommon).ToList();

        var allResources = GetDbResources();

        var childResources = new List<Common.Infrastructure.ClaimSetEditor.ResourceClaim>();
        foreach (var resourceClaims in commonResources.Select(x => x.Children))
            childResources.AddRange(resourceClaims);
        commonResources.AddRange(childResources);

        var currentResources = commonResources
            .Select(r =>
            {
                var resource = allResources.Find(dr => (dr.Name ?? string.Empty).Equals(r.Name, StringComparison.Ordinal));
                if (resource != null)
                {
                    resource.Actions = r.Actions;
                    resource.AuthorizationStrategyOverridesForCRUD = r.AuthorizationStrategyOverridesForCRUD;
                }
                return resource;
            })
            .ToList();

        currentResources.RemoveAll(x => x is null);

        foreach (var resource in currentResources.Where(x => x is not null))
        {
            var editResourceModel = new EditResourceOnClaimSetModel
            {
                ClaimSetId = claimSetId,
                ResourceClaim = MapFromCommon(resource)
            };

            editResourceOnClaimSetCommand.Execute(editResourceModel);

            if (resource!.AuthorizationStrategyOverridesForCRUD != null && resource.AuthorizationStrategyOverridesForCRUD.Any())
            {
                var overrideAuthStrategyModel = new OverrideModel(
                    claimSetId,
                    resource.Id,
                    MapFromCommonStrategies(resource.AuthorizationStrategyOverridesForCRUD));
                overrideDefaultAuthorizationStrategyCommand.Execute(overrideAuthStrategyModel);
            }
        }
    }

    private static ResourceClaim? MapFromCommon(Common.Infrastructure.ClaimSetEditor.ResourceClaim? commonClaim)
    {
        if (commonClaim == null)
            return null;

        return new ResourceClaim
        {
            Id = commonClaim.Id,
            Name = commonClaim.Name,
            ParentId = commonClaim.ParentId,
            ParentName = commonClaim.ParentName,
            IsParent = commonClaim.IsParent,
            Actions = commonClaim.Actions?.Select(a => new ResourceClaimAction
            {
                Name = a.Name,
                Enabled = a.Enabled
            }).ToList(),
            DefaultAuthorizationStrategiesForCRUD = commonClaim.DefaultAuthorizationStrategiesForCRUD
                .Select(x => x == null ? null : MapStrategyFromCommon(x))
                .ToList(),
            AuthorizationStrategyOverridesForCRUD = commonClaim.AuthorizationStrategyOverridesForCRUD
                .Select(x => x == null ? null : MapStrategyFromCommon(x))
                .ToList(),
            Children = commonClaim.Children?.Select(MapFromCommon).Where(c => c != null).Cast<ResourceClaim>().ToList() ?? []
        };
    }

    private static ClaimSetResourceClaimActionAuthStrategies MapStrategyFromCommon(
        Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies commonStrategy)
    {
        return new ClaimSetResourceClaimActionAuthStrategies
        {
            ActionId = commonStrategy.ActionId,
            ActionName = commonStrategy.ActionName,
            AuthorizationStrategies = commonStrategy.AuthorizationStrategies?.Select(a => new AuthorizationStrategy
            {
                AuthStrategyId = a.AuthStrategyId,
                AuthStrategyName = a.AuthStrategyName
            }).ToList()
        };
    }

    private static List<ClaimSetResourceClaimActionAuthStrategies?>? MapFromCommonStrategies(
        List<Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies?>? commonStrategies)
    {
        if (commonStrategies == null)
            return null;

        return commonStrategies
            .Select(x => x == null ? null : MapStrategyFromCommon(x))
            .ToList();
    }

    private sealed class OverrideModel : IOverrideDefaultAuthorizationStrategyModel
    {
        private readonly int _claimSetId;
        private readonly int _resourceClaimId;
        private readonly List<ClaimSetResourceClaimActionAuthStrategies?>? _strategies;

        public OverrideModel(
            int claimSetId,
            int resourceClaimId,
            List<ClaimSetResourceClaimActionAuthStrategies?>? strategies)
        {
            _claimSetId = claimSetId;
            _resourceClaimId = resourceClaimId;
            _strategies = strategies;
        }

        public int ClaimSetId => _claimSetId;
        public int ResourceClaimId => _resourceClaimId;
        public List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides => _strategies;
    }

    private List<Common.Infrastructure.ClaimSetEditor.ResourceClaim> GetDbResources()
    {
        var allResources = new List<Common.Infrastructure.ClaimSetEditor.ResourceClaim>();
        var parentResources = getResourceClaimsQuery.Execute().Select(MapToCommon).ToList();

        foreach (var resource in parentResources)
        {
            AddResourceWithChildren(resource, allResources);
        }

        return allResources;
    }

    private static void AddResourceWithChildren(Common.Infrastructure.ClaimSetEditor.ResourceClaim resource, List<Common.Infrastructure.ClaimSetEditor.ResourceClaim> allResources)
    {
        allResources.Add(resource);

        if (resource.Children != null && resource.Children.Any())
        {
            foreach (var child in resource.Children)
            {
                AddResourceWithChildren(child, allResources);
            }
        }
    }

    private static Common.Infrastructure.ClaimSetEditor.ResourceClaim MapToCommon(ResourceClaim local)
    {
        return new Common.Infrastructure.ClaimSetEditor.ResourceClaim
        {
            Id = local.Id,
            Name = local.Name,
            ParentId = local.ParentId,
            ParentName = local.ParentName,
            IsParent = local.IsParent,
            Actions = local.Actions?.Select(a => new Common.Infrastructure.ClaimSetEditor.ResourceClaimAction
            {
                Name = a.Name,
                Enabled = a.Enabled
            }).ToList(),
            DefaultAuthorizationStrategiesForCRUD = local.DefaultAuthorizationStrategiesForCRUD
                .Select(x => x == null ? null : MapStrategyToCommon(x))
                .ToList(),
            AuthorizationStrategyOverridesForCRUD = local.AuthorizationStrategyOverridesForCRUD
                .Select(x => x == null ? null : MapStrategyToCommon(x))
                .ToList(),
            Children = local.Children?.Select(MapToCommon).ToList() ?? []
        };
    }

    private static Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies MapStrategyToCommon(ClaimSetResourceClaimActionAuthStrategies local)
    {
        return new Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies
        {
            ActionId = local.ActionId,
            ActionName = local.ActionName,
            AuthorizationStrategies = local.AuthorizationStrategies?.Select(a => new Common.Infrastructure.ClaimSetEditor.AuthorizationStrategy
            {
                AuthStrategyId = a.AuthStrategyId,
                AuthStrategyName = a.AuthStrategyName
            }).ToList()
        };
    }
}
