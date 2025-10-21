// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using FluentValidation;

namespace EdFi.Ods.AdminApi.V1.Features.ClaimSets;

public class ResourceClaimValidator
{
    private static List<string>? _duplicateResources;

    public ResourceClaimValidator()
    {
        _duplicateResources = [];
    }

    public void Validate<T>(Lookup<string, ResourceClaim> dbResourceClaims,
        List<string?> dbAuthStrategies, RequestResourceClaimModel resourceClaim, List<ChildrenRequestResourceClaimModel> existingResourceClaims,
        ValidationContext<T> context, string? claimSetName)
    {
        context.MessageFormatter.AppendArgument("ClaimSetName", claimSetName);
        context.MessageFormatter.AppendArgument("ResourceClaimName", resourceClaim.Name);

        var propertyName = "ResourceClaims";

        if (existingResourceClaims.Count(x => x.Name == resourceClaim.Name) > 1)
        {
            if (_duplicateResources != null && resourceClaim.Name != null && !_duplicateResources.Contains(resourceClaim.Name))
            {
                _duplicateResources.Add(resourceClaim.Name);
                context.AddFailure(propertyName, "Only unique resource claims can be added. The following is a duplicate resource: '{ResourceClaimName}'");
            }
        }

        if (!(resourceClaim.Create || resourceClaim.Delete || resourceClaim.Read || resourceClaim.Update || resourceClaim.ReadChanges))
        {
            context.AddFailure(propertyName, "Only valid resources can be added. A resource must have at least one action associated with it to be added. The following is an invalid resource: '{ResourceClaimName}'");
        }

        var resources = dbResourceClaims[resourceClaim.Name!.ToLower()].ToList();
        if (resources.Count == 0)
        {
            context.AddFailure(propertyName, "This Claim Set contains a resource which is not in the system. Claimset Name: '{ClaimSetName}' Resource name: '{ResourceClaimName}'.\n");
        }

        if (resourceClaim.AuthStrategyOverridesForCRUD.Length != 0)
        {
            if (resourceClaim.AuthStrategyOverridesForCRUD.Length < 5)
                context.AddFailure(propertyName, "Please provide a list of 5 elements for 'AuthStrategyOverridesForCRUD' in the Resource name: '{ResourceClaimName}'");
        }

        if (resourceClaim.Children.Count != 0)
        {
            foreach (var child in resourceClaim.Children)
            {
                var childResources = dbResourceClaims[child.Name!.ToLower()].ToList();
                if (childResources.Count != 0)
                {
                    foreach (var childResource in childResources)
                    {
                        context.MessageFormatter.AppendArgument("ChildResource", childResource.Name);
                        if (childResource.ParentId == 0)
                        {
                            context.AddFailure(propertyName, "'{ChildResource}' can not be added as a child resource.");
                        }

                        else if (!resources.Where(x => x is not null).Select(x => x.Id).Contains(childResource.ParentId))
                        {
                            context.MessageFormatter.AppendArgument("CorrectParentResource", childResource.ParentName);
                            context.AddFailure(propertyName, "Child resource: '{ChildResource}' added to the wrong parent resource. Correct parent resource is: '{CorrectParentResource}'");
                        }
                    }
                }
                Validate(dbResourceClaims, dbAuthStrategies, child, resourceClaim.Children, context, claimSetName);
            }
        }
    }
}
