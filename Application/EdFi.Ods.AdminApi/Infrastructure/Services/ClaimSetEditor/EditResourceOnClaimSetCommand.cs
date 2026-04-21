// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using Microsoft.EntityFrameworkCore;
using CommonResourceClaim = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaim;
using CommonResourceClaimAction = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaimAction;
using CommonAuthorizationStrategy = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.AuthorizationStrategy;
using CommonClaimSetResourceClaimActionAuthStrategies = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public class EditResourceOnClaimSetCommand(ISecurityContext context)
    : EditResourceOnClaimSetCommandBase(context)
{
    public void Execute(IEditResourceOnClaimSetModel model)
    {
        ExecuteCore(new EditResourceOnClaimSetModelCommonAdapter
        {
            ClaimSetId = model.ClaimSetId,
            ResourceClaim = model.ResourceClaim == null ? null : Map(model.ResourceClaim)
        });
    }

    private sealed class EditResourceOnClaimSetModelCommonAdapter : IEditResourceOnClaimSetModelCommon
    {
        public int ClaimSetId { get; set; }
        public CommonResourceClaim? ResourceClaim { get; set; }
    }

    private static CommonResourceClaim Map(ResourceClaim resourceClaim)
    {
        return new CommonResourceClaim
        {
            Id = resourceClaim.Id,
            ParentId = resourceClaim.ParentId,
            ParentName = resourceClaim.ParentName,
            Name = resourceClaim.Name,
            IsParent = resourceClaim.IsParent,
            Actions = resourceClaim.Actions?.Select(Map).ToList(),
            DefaultAuthorizationStrategiesForCRUD = resourceClaim.DefaultAuthorizationStrategiesForCRUD
                .Select(x => x == null ? null : Map(x))
                .ToList(),
            AuthorizationStrategyOverridesForCRUD = resourceClaim.AuthorizationStrategyOverridesForCRUD
                .Select(x => x == null ? null : Map(x))
                .ToList(),
            Children = resourceClaim.Children.Select(Map).ToList()
        };
    }

    private static CommonResourceClaimAction Map(ResourceClaimAction action)
    {
        return new CommonResourceClaimAction
        {
            Name = action.Name,
            Enabled = action.Enabled
        };
    }

    private static CommonClaimSetResourceClaimActionAuthStrategies Map(ClaimSetResourceClaimActionAuthStrategies authorizationStrategy)
    {
        return new CommonClaimSetResourceClaimActionAuthStrategies
        {
            ActionId = authorizationStrategy.ActionId,
            ActionName = authorizationStrategy.ActionName,
            AuthorizationStrategies = authorizationStrategy.AuthorizationStrategies?.Select(Map).ToList()
        };
    }

    private static CommonAuthorizationStrategy Map(AuthorizationStrategy strategy)
    {
        return new CommonAuthorizationStrategy
        {
            AuthStrategyId = strategy.AuthStrategyId,
            AuthStrategyName = strategy.AuthStrategyName,
            IsInheritedFromParent = strategy.IsInheritedFromParent
        };
    }
}

public interface IEditResourceOnClaimSetModel
{
    int ClaimSetId { get; }
    ResourceClaim? ResourceClaim { get; }
}

public class EditResourceOnClaimSetModel : IEditResourceOnClaimSetModel
{
    public int ClaimSetId { get; set; }
    public ResourceClaim? ResourceClaim { get; set; }
}

