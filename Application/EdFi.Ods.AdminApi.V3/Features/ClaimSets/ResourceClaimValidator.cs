// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using FluentValidation;

namespace EdFi.Ods.AdminApi.V3.Features.ClaimSets;

public static class ResourceClaimValidator
{
    private static List<string>? _duplicateResources = [];

    public static void Validate<T>(Lookup<string, ResourceClaim> dbResourceClaims, List<string> dbActions,
        List<string?> dbAuthStrategies, ClaimSetResourceClaimModel resourceClaim, List<ClaimSetResourceClaimModel> existingResourceClaims,
        ValidationContext<T> context, string? claimSetName)
    {
        context.MessageFormatter.AppendArgument("ClaimSetName", claimSetName);
        context.MessageFormatter.AppendArgument("ResourceClaimName", resourceClaim.Name);

        var propertyName = "ResourceClaims";
        ValidateDuplicateResourceClaim(resourceClaim, existingResourceClaims, context, propertyName);

        ValidateCRUD(resourceClaim.Actions, dbActions, context, propertyName);

        var resources = dbResourceClaims[resourceClaim.Name!.ToLower()].ToList();
        ValidateIfExist(context, propertyName, resources);
        ValidateAuthStrategies(dbAuthStrategies, resourceClaim, context, propertyName);
        ValidateAuthStrategiesOverride(dbAuthStrategies, resourceClaim, context, propertyName);
        ValidateParentClaimName(resourceClaim, context, propertyName, resources);
    }

    public static void Validate<T>(Lookup<int, ResourceClaim> dbResourceClaims, List<string> dbActions, IResourceClaimOnClaimSetRequest editResourceClaimOnClaimSetRequest,
        ValidationContext<T> context, string? claimSetName)
    {
        context.MessageFormatter.AppendArgument("ClaimSetName", claimSetName);
        context.MessageFormatter.AppendArgument("ResourceClaimName", editResourceClaimOnClaimSetRequest.ResourceClaimId);

        var propertyName = "ResourceClaimActions";
        var resources = dbResourceClaims[editResourceClaimOnClaimSetRequest.ResourceClaimId].ToList();
        ValidateIfExist(context, propertyName, resources);
        ValidateCRUD(editResourceClaimOnClaimSetRequest.ResourceClaimActions, dbActions, context, propertyName);
    }

    private static void ValidateIfExist<T>(ValidationContext<T> context, string propertyName, List<ResourceClaim> resources)
    {
        if (!resources.Any())
        {
            context.AddFailure(propertyName, "This Claim Set contains a resource which is not in the system. Claimset Name: '{ClaimSetName}' Resource: '{ResourceClaimName}'.");
        }
    }

    private static void ValidateDuplicateResourceClaim<T>(ClaimSetResourceClaimModel resourceClaim, List<ClaimSetResourceClaimModel> existingResourceClaims, ValidationContext<T> context, string propertyName)
    {
        if (existingResourceClaims.Count(x => x.Name == resourceClaim.Name) > 1)
        {
            if (_duplicateResources == null || resourceClaim.Name == null ||
                _duplicateResources.Contains(resourceClaim.Name))
            {
                return;
            }

            _duplicateResources.Add(resourceClaim.Name);
            context.AddFailure(propertyName, "Only unique resource claims can be added. The following is a duplicate resource: '{ResourceClaimName}'.");
        }
    }

    private static void ValidateParentClaimName<T>(ClaimSetResourceClaimModel resourceClaim,
        ValidationContext<T> context, string propertyName, List<ResourceClaim> resources)
    {
        if (resourceClaim.ParentClaimName == null)
        {
            return;
        }

        foreach (var resource in resources)
        {
            context.MessageFormatter.AppendArgument("ChildResource", resource.Name);

            if (resource.ParentClaimName == null)
            {
                context.AddFailure(propertyName, "'{ChildResource}' can not be added as a child resource.");
            }
            else if (!resource.ParentClaimName.Equals(resourceClaim.ParentClaimName, StringComparison.OrdinalIgnoreCase))
            {
                context.MessageFormatter.AppendArgument("CorrectParentResource", resource.ParentName);
                context.AddFailure(propertyName, "Child resource: '{ChildResource}' added to the wrong parent resource. Correct parent resource is: '{CorrectParentResource}'.");
            }
        }
    }

    private static void ValidateAuthStrategiesOverride<T>(List<string?> dbAuthStrategies,
        ClaimSetResourceClaimModel resourceClaim, ValidationContext<T> context, string propertyName)
    {
        if (resourceClaim.AuthorizationStrategyOverrides != null && resourceClaim.AuthorizationStrategyOverrides.Any())
        {
            foreach (var authStrategyOverrideWithAction in resourceClaim.AuthorizationStrategyOverrides)
            {
                if (authStrategyOverrideWithAction?.AuthorizationStrategies != null)
                {
                    foreach (var authStrategyOverride in authStrategyOverrideWithAction.AuthorizationStrategies)
                    {
                        if (authStrategyOverride?.AuthStrategyName != null && !dbAuthStrategies.Contains(authStrategyOverride.AuthStrategyName))
                        {
                            context.MessageFormatter.AppendArgument("AuthStrategyName", authStrategyOverride.AuthStrategyName);
                            context.AddFailure(propertyName, "This resource claim contains an authorization strategy which is not in the system. Claimset Name: '{ClaimSetName}' Resource name: '{ResourceClaimName}' Authorization strategy: '{AuthStrategyName}'.");
                        }
                    }
                }
            }
        }
    }

    private static void ValidateAuthStrategies<T>(List<string?> dbAuthStrategies,
        ClaimSetResourceClaimModel resourceClaim, ValidationContext<T> context, string propertyName)
    {
        if (resourceClaim.DefaultAuthorizationStrategies != null && resourceClaim.DefaultAuthorizationStrategies.Any())
        {
            foreach (var defaultASWithAction in resourceClaim.DefaultAuthorizationStrategies)
            {
                if (defaultASWithAction?.AuthorizationStrategies == null)
                {
                    continue;
                }

                foreach (var defaultAS in defaultASWithAction.AuthorizationStrategies)
                {
                    if (defaultAS?.AuthStrategyName != null && !dbAuthStrategies.Contains(defaultAS.AuthStrategyName))
                    {
                        context.MessageFormatter.AppendArgument("AuthStrategyName", defaultAS.AuthStrategyName);
                        context.AddFailure(propertyName, "This resource claim contains an authorization strategy which is not in the system. Claimset Name: '{ClaimSetName}' Resource name: '{ResourceClaimName}' Authorization strategy: '{AuthStrategyName}'.");
                    }
                }
            }
        }
    }

    private static void ValidateCRUD<T>(List<ResourceClaimAction>? resourceClaimActions,
        List<string> dbActions, ValidationContext<T> context, string propertyName)
    {
        if (resourceClaimActions != null && resourceClaimActions.Any())
        {
            var atleastAnActionEnabled = resourceClaimActions.Exists(x => x.Enabled);
            if (!atleastAnActionEnabled)
            {
                context.AddFailure(propertyName, FeatureConstants.ResourceClaimOneActionNotSet);
            }
            else
            {
                var duplicates = resourceClaimActions.GroupBy(x => x.Name)
                              .Where(g => g.Count() > 1)
                              .Select(y => y.Key)
                              .ToList();
                foreach (var duplicate in duplicates)
                {
                    context.AddFailure(propertyName, $"{duplicate} action is duplicated.");
                }
                foreach (var action in resourceClaimActions.Select(x => x.Name))
                {
                    if (!dbActions.Exists(actionName => actionName != null &&
                        actionName.Equals(action, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        context.AddFailure(propertyName, $"{action} is not a valid action.");
                    }
                }
            }
        }
        else
        {
            context.AddFailure(propertyName, $"Actions can not be empty.");
        }
    }
}


