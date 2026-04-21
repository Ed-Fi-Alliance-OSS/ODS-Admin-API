// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.Features.ClaimSets;

public static class ClaimSetMapper
{
    public static ClaimSetModel ToModel(ClaimSet source)
    {
        return new ClaimSetModel
        {
            Id = source.Id,
            Name = source.Name,
            IsSystemReserved = !source.IsEditable
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
            Applications = model.Applications
        };
    }

    public static List<ClaimSetModel> ToModelList(IEnumerable<ClaimSet> source)
    {
        return source.Select(ToModel).ToList();
    }

    public static SimpleApplicationModel ToSimpleApplicationModel(Application source)
    {
        return new SimpleApplicationModel
        {
            ApplicationName = source.Name
        };
    }

    public static List<SimpleApplicationModel> ToSimpleApplicationModelList(IEnumerable<Application> source)
    {
        return source.Select(ToSimpleApplicationModel).ToList();
    }

    public static ClaimSetResourceClaimModel ToClaimSetResourceClaimModel(ResourceClaim source)
    {
        return new ClaimSetResourceClaimModel
        {
            Id = source.Id,
            Name = source.Name,
            Actions = source.Actions,
            DefaultAuthorizationStrategiesForCRUD = source.DefaultAuthorizationStrategiesForCRUD,
            AuthorizationStrategyOverridesForCRUD = source.AuthorizationStrategyOverridesForCRUD,
            Children = ToClaimSetResourceClaimModelList(source.Children)
        };
    }

    public static List<ClaimSetResourceClaimModel> ToClaimSetResourceClaimModelList(IEnumerable<ResourceClaim> source)
    {
        return source.Select(ToClaimSetResourceClaimModel).ToList();
    }

    public static ResourceClaim ToResourceClaim(ClaimSetResourceClaimModel source)
    {
        return new ResourceClaim
        {
            Id = source.Id,
            Name = source.Name,
            Actions = source.Actions,
            DefaultAuthorizationStrategiesForCRUD = source.DefaultAuthorizationStrategiesForCRUD,
            AuthorizationStrategyOverridesForCRUD = source.AuthorizationStrategyOverridesForCRUD,
            Children = ToResourceClaimList(source.Children)
        };
    }

    public static List<ResourceClaim> ToResourceClaimList(IEnumerable<ClaimSetResourceClaimModel> source)
    {
        return source.Select(ToResourceClaim).ToList();
    }

    public static EditResourceOnClaimSetModel ToEditResourceOnClaimSetModel(IResourceClaimOnClaimSetRequest source)
    {
        return new EditResourceOnClaimSetModel
        {
            ClaimSetId = source.ClaimSetId,
            ResourceClaim = new ResourceClaim
            {
                Id = source.ResourceClaimId,
                Actions = source.ResourceClaimActions
            }
        };
    }

    public static Common.Infrastructure.ClaimSetEditor.OverrideAuthStrategyOnClaimSetModel ToOverrideAuthStrategyOnClaimSetModel(
        ResourceClaims.EditAuthStrategy.OverrideAuthStategyOnClaimSetRequest source,
        List<int> authStrategyIds)
    {
        return new Common.Infrastructure.ClaimSetEditor.OverrideAuthStrategyOnClaimSetModel
        {
            ClaimSetId = source.ClaimSetId,
            ResourceClaimId = source.ResourceClaimId,
            ActionName = source.ActionName,
            AuthStrategyIds = authStrategyIds
        };
    }

    public static AuthorizationStrategy ToAuthorizationStrategy(SecurityAuthorizationStrategy source, bool isInheritedFromParent = false)
    {
        return new AuthorizationStrategy
        {
            AuthStrategyId = source.AuthorizationStrategyId,
            AuthStrategyName = source.AuthorizationStrategyName,
            IsInheritedFromParent = isInheritedFromParent
        };
    }

    public static List<AuthorizationStrategy> ToAuthorizationStrategyList(IEnumerable<SecurityAuthorizationStrategy> source, bool isInheritedFromParent = false)
    {
        return source.Select(item => ToAuthorizationStrategy(item, isInheritedFromParent)).ToList();
    }
}
