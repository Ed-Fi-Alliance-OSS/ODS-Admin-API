// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public class AddOrEditResourcesOnClaimSetCommand
{
    private readonly EditResourceOnClaimSetCommand _editResourceOnClaimSetCommand;
    private readonly IGetResourceClaimsQuery _getResourceClaimsQuery;
    private readonly OverrideDefaultAuthorizationStrategyCommand _overrideDefaultAuthorizationStrategyCommand;

    public AddOrEditResourcesOnClaimSetCommand(EditResourceOnClaimSetCommand editResourceOnClaimSetCommand,
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
                var overrideAuthStrategyModel = new OverrideAuthorizationStrategyModel
                {
                    ClaimSetId = claimSetId,
                    ResourceClaimId = resource.Id,
                    ClaimSetResourceClaimActionAuthStrategyOverrides = resource.AuthorizationStrategyOverridesForCRUD
                };
                _overrideDefaultAuthorizationStrategyCommand.Execute(overrideAuthStrategyModel);
            }
        }
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

    private void AddResourceWithChildren(ResourceClaim resource, List<ResourceClaim> allResources)
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

public class AddClaimSetModel : IAddClaimSetModel
{
    public string? ClaimSetName { get; set; }
}

public class EditResourceOnClaimSetModel : IEditResourceOnClaimSetModel
{
    public int ClaimSetId { get; set; }
    public ResourceClaim? ResourceClaim { get; set; }
}

public class OverrideAuthorizationStrategyModel : IOverrideDefaultAuthorizationStrategyModel
{
    public int ClaimSetId { get; set; }
    public int ResourceClaimId { get; set; }
    public List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides { get; set; }
}
