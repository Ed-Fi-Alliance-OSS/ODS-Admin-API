// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public class AddOrEditResourcesOnClaimSetCommand
{
    private readonly EditResourceOnClaimSetCommand _editResourceOnClaimSetCommand;
    private readonly IGetResourceClaimsQuery _getResourceClaimsQuery;
    private readonly OverrideDefaultAuthorizationStrategyCommand _overrideDefaultAuthorizationStrategyCommand;

    public AddOrEditResourcesOnClaimSetCommand(
        EditResourceOnClaimSetCommand editResourceOnClaimSetCommand,
        IGetResourceClaimsQuery getResourceClaimsQuery,
        OverrideDefaultAuthorizationStrategyCommand overrideDefaultAuthorizationStrategyCommand)
    {
        _editResourceOnClaimSetCommand = editResourceOnClaimSetCommand;
        _getResourceClaimsQuery = getResourceClaimsQuery;
        _overrideDefaultAuthorizationStrategyCommand = overrideDefaultAuthorizationStrategyCommand;
    }

    public void Execute(int claimSetId, List<ResourceClaim> resources)
    {
        var allResources = GetDbResources();

        var childResources = new List<ResourceClaim>();
        foreach (var resourceClaims in resources.Select(x => x.Children))
            childResources.AddRange(resourceClaims);
        resources.AddRange(childResources);

        var currentResources = resources.Select(r =>
            {
                var resource = allResources.Find(dr => (dr.Name ?? string.Empty).Equals(r.Name, StringComparison.Ordinal));
                if (resource != null)
                {
                    resource.Actions = r.Actions;
                    resource.AuthorizationStrategyOverridesForCRUD = r.AuthorizationStrategyOverridesForCRUD;
                }
                return resource;
            }).ToList();

        currentResources.RemoveAll(x => x is null);

        foreach (var resource in currentResources.Where(x => x is not null))
        {
            var editResourceModel = new EditResourceOnClaimSetModel
            {
                ClaimSetId = claimSetId,
                ResourceClaim = resource
            };

            _editResourceOnClaimSetCommand.Execute(editResourceModel);

            if (resource!.AuthorizationStrategyOverridesForCRUD != null && resource.AuthorizationStrategyOverridesForCRUD.Any())
            {
                var overrideAuthStrategyModel = new LocalOverrideModel(
                    claimSetId,
                    resource.Id,
                    resource.AuthorizationStrategyOverridesForCRUD);
                _overrideDefaultAuthorizationStrategyCommand.Execute(overrideAuthStrategyModel);
            }
        }
    }

    private sealed class LocalOverrideModel : IOverrideDefaultAuthorizationStrategyModel
    {
        private readonly int _claimSetId;
        private readonly int _resourceClaimId;
        private readonly List<ClaimSetResourceClaimActionAuthStrategies?>? _strategies;

        public LocalOverrideModel(int claimSetId, int resourceClaimId, List<ClaimSetResourceClaimActionAuthStrategies?>? strategies)
        {
            _claimSetId = claimSetId;
            _resourceClaimId = resourceClaimId;
            _strategies = strategies;
        }

        public int ClaimSetId => _claimSetId;
        public int ResourceClaimId => _resourceClaimId;
        public List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides => _strategies;
    }

    private List<ResourceClaim> GetDbResources()
    {
        var allResources = new List<ResourceClaim>();
        var parentResources = _getResourceClaimsQuery.Execute().ToList();

        foreach (var resource in parentResources)
        {
            AddResourceWithChildren(resource, allResources);
        }

        return allResources;
    }

    private static void AddResourceWithChildren(ResourceClaim resource, List<ResourceClaim> allResources)
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
}
