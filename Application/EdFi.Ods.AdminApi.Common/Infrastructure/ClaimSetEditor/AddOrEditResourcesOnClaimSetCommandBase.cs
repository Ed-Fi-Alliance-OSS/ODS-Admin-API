// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

public interface IEditResourceOnClaimSetCommandCommon
{
    void Execute(IEditResourceOnClaimSetModelCommon model);
}

public interface IGetResourceClaimsQueryCommon
{
    IEnumerable<ResourceClaim> Execute();
}

public interface IOverrideDefaultAuthorizationStrategyCommandCommon
{
    void Execute(IOverrideDefaultAuthorizationStrategyModel model);
}

public abstract class AddOrEditResourcesOnClaimSetCommandBase(
    IEditResourceOnClaimSetCommandCommon editResourceOnClaimSetCommand,
    IGetResourceClaimsQueryCommon getResourceClaimsQuery,
    IOverrideDefaultAuthorizationStrategyCommandCommon overrideDefaultAuthorizationStrategyCommand)
{
    protected readonly IEditResourceOnClaimSetCommandCommon _editResourceOnClaimSetCommand = editResourceOnClaimSetCommand;
    protected readonly IGetResourceClaimsQueryCommon _getResourceClaimsQuery = getResourceClaimsQuery;
    protected readonly IOverrideDefaultAuthorizationStrategyCommandCommon _overrideDefaultAuthorizationStrategyCommand = overrideDefaultAuthorizationStrategyCommand;

    protected void ExecuteCore(int claimSetId, List<ResourceClaim> resources)
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
                var overrideAuthStrategyModel = new OverrideAuthStrategyOnClaimSetModelImpl
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

public class EditResourceOnClaimSetModel : IEditResourceOnClaimSetModelCommon
{
    public int ClaimSetId { get; set; }
    public ResourceClaim? ResourceClaim { get; set; }
}

public class OverrideAuthStrategyOnClaimSetModelImpl : IOverrideDefaultAuthorizationStrategyModel
{
    public int ClaimSetId { get; set; }
    public int ResourceClaimId { get; set; }
    public List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides { get; set; }
}
