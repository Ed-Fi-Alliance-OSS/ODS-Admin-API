// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor.Extensions;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using FluentValidation;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

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
                var resource = allResources.FirstOrDefault(dr => (dr.Name ?? string.Empty).Equals(r.Name, StringComparison.Ordinal));
                if (resource != null)
                {
                    resource.Create = r.Create;
                    resource.Read = r.Read;
                    resource.Update = r.Update;
                    resource.Delete = r.Delete;
                    resource.ReadChanges = r.ReadChanges;
                    resource.AuthStrategyOverridesForCRUD = r.AuthStrategyOverridesForCRUD;
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

            if (resource!.AuthStrategyOverridesForCRUD != null && resource.AuthStrategyOverridesForCRUD.Any())
            {
                var overrideAuthStrategyModel = new OverrideAuthorizationStrategyModel
                {
                    ClaimSetId = claimSetId,
                    ResourceClaimId = resource.Id,
                    AuthorizationStrategyForCreate = AuthStrategyOverrideForAction(resource.AuthStrategyOverridesForCRUD.Create()),
                    AuthorizationStrategyForRead = AuthStrategyOverrideForAction(resource.AuthStrategyOverridesForCRUD.Read()),
                    AuthorizationStrategyForUpdate = AuthStrategyOverrideForAction(resource.AuthStrategyOverridesForCRUD.Update()),
                    AuthorizationStrategyForDelete = AuthStrategyOverrideForAction(resource.AuthStrategyOverridesForCRUD.Delete()),
                    AuthorizationStrategyForReadChanges = AuthStrategyOverrideForAction(resource.AuthStrategyOverridesForCRUD.ReadChanges())
                };
                _overrideDefaultAuthorizationStrategyCommand.Execute(overrideAuthStrategyModel);
            }
        }

        static int[] AuthStrategyOverrideForAction(ClaimSetResourceClaimActionAuthStrategies? claimSetResourceClaimActionAuthStrategies)
        {
            if (claimSetResourceClaimActionAuthStrategies != null && claimSetResourceClaimActionAuthStrategies.AuthorizationStrategies != null)
            {
                return claimSetResourceClaimActionAuthStrategies.AuthorizationStrategies.Where(p => p is not null).Select(p => p!.AuthStrategyId).ToArray();
            }
            return Array.Empty<int>();
        }
    }

    private List<ResourceClaim> GetDbResources()
    {
        var allResources = new List<ResourceClaim>();
        var parentResources = _getResourceClaimsQuery.Execute().ToList();
        allResources.AddRange(parentResources);
        foreach (var children in parentResources.Select(x => x.Children))
        {
            allResources.AddRange(children);
        }

        return allResources;
    }
}

public class AddClaimSetModel : IAddClaimSetModel
{
    public string ClaimSetName { get; set; } = string.Empty;
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
    public int[]? AuthorizationStrategyForCreate { get; set; }
    public int[]? AuthorizationStrategyForRead { get; set; }
    public int[]? AuthorizationStrategyForUpdate { get; set; }
    public int[]? AuthorizationStrategyForDelete { get; set; }
    public int[]? AuthorizationStrategyForReadChanges { get; set; }

}
