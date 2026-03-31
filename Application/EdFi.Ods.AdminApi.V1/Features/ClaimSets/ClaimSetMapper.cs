// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using SecurityAuthorizationStrategy = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.V1.Features.ClaimSets;

public static class ClaimSetMapper
{
    public static ClaimSetModel ToModel(ClaimSet source)
    {
        return new ClaimSetModel
        {
            Id = source.Id,
            Name = source.Name,
            IsSystemReserved = !source.IsEditable,
        };
    }

    public static ClaimSetDetailsModel ToDetailsModel(ClaimSet source)
    {
        var model = ToModel(source);

        return new ClaimSetDetailsModel
        {
            Id = model.Id,
            Name = model.Name,
            IsSystemReserved = model.IsSystemReserved,
            ApplicationsCount = model.ApplicationsCount,
        };
    }

    public static List<ClaimSetModel> ToModelList(IEnumerable<ClaimSet> source)
    {
        return source.Select(ToModel).ToList();
    }

    public static ResourceClaimModel ToResourceClaimModel(ResourceClaim source)
    {
        return new ResourceClaimModel
        {
            Name = source.Name,
            Create = source.Create,
            Read = source.Read,
            Update = source.Update,
            Delete = source.Delete,
            ReadChanges = source.ReadChanges,
            AuthStrategyOverridesForCRUD = ToAuthorizationStrategiesModelArray(source.AuthStrategyOverridesForCRUD),
            DefaultAuthStrategiesForCRUD = ToAuthorizationStrategiesModelArray(source.DefaultAuthStrategiesForCRUD),
            Children = ToResourceClaimModelList(source.Children),
        };
    }

    public static List<ResourceClaimModel> ToResourceClaimModelList(IEnumerable<ResourceClaim> source)
    {
        return source.Select(ToResourceClaimModel).ToList();
    }

    public static ResourceClaim ToResourceClaim(RequestResourceClaimModel source)
    {
        // Use pattern matching to read the correct children collection, accounting for property shadowing
        IEnumerable<RequestResourceClaimModel> childrenToMap = source is ChildrenRequestResourceClaimModel childModel
            ? childModel.Children  // Use the shadowed property (List<RequestResourceClaimModel>)
            : source.Children.Cast<RequestResourceClaimModel>();  // Convert base property (List<ChildrenRequestResourceClaimModel>)

        return new ResourceClaim
        {
            Name = source.Name,
            Create = source.Create,
            Read = source.Read,
            Update = source.Update,
            Delete = source.Delete,
            ReadChanges = source.ReadChanges,
            AuthStrategyOverridesForCRUD = ToClaimSetResourceClaimActionAuthStrategiesArray(source.AuthStrategyOverridesForCRUD),
            Children = childrenToMap.Select(ToResourceClaim).ToList(),
        };
    }

    public static List<ResourceClaim> ToResourceClaimList(IEnumerable<RequestResourceClaimModel> source)
    {
        return source.Select(ToResourceClaim).ToList();
    }

    public static ChildrenRequestResourceClaimModel ToChildrenRequestResourceClaimModel(RequestResourceClaimModel source)
    {
        var model = new ChildrenRequestResourceClaimModel
        {
            Name = source.Name,
            Create = source.Create,
            Read = source.Read,
            Update = source.Update,
            Delete = source.Delete,
            ReadChanges = source.ReadChanges,
            AuthStrategyOverridesForCRUD = source.AuthStrategyOverridesForCRUD,
        };

        model.Children = GetChildrenForMapping(source).Select(ToRequestResourceClaimModel).ToList();

        return model;
    }

    private static RequestResourceClaimModel ToRequestResourceClaimModel(RequestResourceClaimModel source)
    {
        return new RequestResourceClaimModel
        {
            Name = source.Name,
            Create = source.Create,
            Read = source.Read,
            Update = source.Update,
            Delete = source.Delete,
            ReadChanges = source.ReadChanges,
            AuthStrategyOverridesForCRUD = source.AuthStrategyOverridesForCRUD,
            Children = GetChildrenForMapping(source).Select(ToChildrenRequestResourceClaimModel).ToList(),
        };
    }

    /// <summary>
    /// Retrieves children from either the base or shadowed property based on runtime type.
    /// ChildrenRequestResourceClaimModel shadows Children with a different type, so direct access
    /// through the base type reference would read the wrong property. This helper detects the
    /// actual runtime type and reads the correct collection.
    /// </summary>
    private static IEnumerable<RequestResourceClaimModel> GetChildrenForMapping(RequestResourceClaimModel source)
    {
        return source is ChildrenRequestResourceClaimModel childModel
            ? childModel.Children  // Shadowed property (List<RequestResourceClaimModel>)
            : source.Children;     // Base property (List<ChildrenRequestResourceClaimModel>, which is a subtype)
    }

    public static List<ChildrenRequestResourceClaimModel> ToChildrenRequestResourceClaimModelList(IEnumerable<RequestResourceClaimModel> source)
    {
        return source.Select(ToChildrenRequestResourceClaimModel).ToList();
    }

    public static AuthorizationStrategyModel ToAuthorizationStrategyModel(AuthorizationStrategy source)
    {
        return new AuthorizationStrategyModel
        {
            AuthStrategyId = source.AuthStrategyId,
            AuthStrategyName = source.AuthStrategyName,
            DisplayName = source.DisplayName,
            IsInheritedFromParent = source.IsInheritedFromParent,
        };
    }

    public static AuthorizationStrategy ToAuthorizationStrategy(AuthorizationStrategyModel source)
    {
        return new AuthorizationStrategy
        {
            AuthStrategyId = source.AuthStrategyId,
            AuthStrategyName = source.AuthStrategyName,
            DisplayName = source.DisplayName,
            IsInheritedFromParent = source.IsInheritedFromParent,
        };
    }

    public static AuthorizationStrategy ToAuthorizationStrategy(SecurityAuthorizationStrategy source, bool isInheritedFromParent = false)
    {
        return new AuthorizationStrategy
        {
            AuthStrategyId = source.AuthorizationStrategyId,
            AuthStrategyName = source.AuthorizationStrategyName,
            IsInheritedFromParent = isInheritedFromParent,
        };
    }

    public static List<AuthorizationStrategy> ToAuthorizationStrategyList(IEnumerable<SecurityAuthorizationStrategy> source, bool isInheritedFromParent = false)
    {
        return source.Select(item => ToAuthorizationStrategy(item, isInheritedFromParent)).ToList();
    }

    public static ClaimSetResourceClaimActionAuthStrategies ToClaimSetResourceClaimActionAuthStrategies(AuthorizationStrategiesModel source)
    {
        return new ClaimSetResourceClaimActionAuthStrategies
        {
            AuthorizationStrategies = source.AuthorizationStrategies?.Where(x => x is not null).Select(x => ToAuthorizationStrategy(x!)).ToArray() ?? Array.Empty<AuthorizationStrategy>(),
        };
    }

    public static AuthorizationStrategiesModel ToAuthorizationStrategiesModel(ClaimSetResourceClaimActionAuthStrategies source)
    {
        return new AuthorizationStrategiesModel
        {
            AuthorizationStrategies = source.AuthorizationStrategies?
                .Where(item => item is not null)
                .Select(item => ToAuthorizationStrategyModel(item!))
                .ToArray() ?? Array.Empty<AuthorizationStrategyModel>(),
        };
    }

    public static ClaimSetResourceClaimActionAuthStrategies?[] ToClaimSetResourceClaimActionAuthStrategiesArray(AuthorizationStrategiesModel?[]? source)
    {
        return source?.Select(item => item is null ? null : ToClaimSetResourceClaimActionAuthStrategies(item)).ToArray() ?? Array.Empty<ClaimSetResourceClaimActionAuthStrategies?>();
    }

    public static AuthorizationStrategiesModel?[] ToAuthorizationStrategiesModelArray(ClaimSetResourceClaimActionAuthStrategies?[]? source)
    {
        return source?.Select(item => item is null ? null : ToAuthorizationStrategiesModel(item)).ToArray() ?? Array.Empty<AuthorizationStrategiesModel?>();
    }
}