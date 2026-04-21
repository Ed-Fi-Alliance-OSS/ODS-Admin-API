// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public class UpdateResourcesOnClaimSetCommand : UpdateResourcesOnClaimSetCommandBase
{
    public UpdateResourcesOnClaimSetCommand(
        ISecurityContext context,
        AddOrEditResourcesOnClaimSetCommand addOrEditResourcesOnClaimSetCommand)
        : base(context, new AddOrEditResourcesOnClaimSetCommandAdapter(addOrEditResourcesOnClaimSetCommand))
    {
    }

    public void Execute(IUpdateResourcesOnClaimSetModel model)
    {
        var localResources = model.ResourceClaims?
            .Select(r => new Common.Infrastructure.ClaimSetEditor.ResourceClaim
            {
                Id = r.Id,
                Name = r.Name,
                ParentId = r.ParentId,
                ParentName = r.ParentName,
                IsParent = r.IsParent,
                Actions = r.Actions?.Select(a => new Common.Infrastructure.ClaimSetEditor.ResourceClaimAction
                {
                    Name = a.Name,
                    Enabled = a.Enabled
                }).ToList(),
                DefaultAuthorizationStrategiesForCRUD = r.DefaultAuthorizationStrategiesForCRUD
                    .Select(x => x == null ? null : MapStrategy(x))
                    .ToList(),
                AuthorizationStrategyOverridesForCRUD = r.AuthorizationStrategyOverridesForCRUD
                    .Select(x => x == null ? null : MapStrategy(x))
                    .ToList(),
                Children = r.Children?.Select(MapResourceClaim).ToList() ?? []
            })
            .ToList();

        ExecuteCore(model.ClaimSetId, localResources);
    }

    private static Common.Infrastructure.ClaimSetEditor.ResourceClaim MapResourceClaim(ResourceClaim local)
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
                .Select(x => x == null ? null : MapStrategy(x))
                .ToList(),
            AuthorizationStrategyOverridesForCRUD = local.AuthorizationStrategyOverridesForCRUD
                .Select(x => x == null ? null : MapStrategy(x))
                .ToList(),
            Children = local.Children?.Select(MapResourceClaim).ToList() ?? []
        };
    }

    private static Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies MapStrategy(ClaimSetResourceClaimActionAuthStrategies local)
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

    private sealed class AddOrEditResourcesOnClaimSetCommandAdapter : IAddOrEditResourcesOnClaimSetCommandCommon
    {
        private readonly AddOrEditResourcesOnClaimSetCommand _command;

        public AddOrEditResourcesOnClaimSetCommandAdapter(AddOrEditResourcesOnClaimSetCommand command)
        {
            _command = command;
        }

        public void Execute(int claimSetId, List<Common.Infrastructure.ClaimSetEditor.ResourceClaim> resources)
        {
            var localResources = resources
                .Select(r => new ResourceClaim
                {
                    Id = r.Id,
                    Name = r.Name,
                    ParentId = r.ParentId,
                    ParentName = r.ParentName,
                    IsParent = r.IsParent,
                    Actions = r.Actions?.Select(a => new ResourceClaimAction
                    {
                        Name = a.Name,
                        Enabled = a.Enabled
                    }).ToList(),
                    DefaultAuthorizationStrategiesForCRUD = r.DefaultAuthorizationStrategiesForCRUD
                        .Select(x => x == null ? null : UnmapStrategy(x))
                        .ToList(),
                    AuthorizationStrategyOverridesForCRUD = r.AuthorizationStrategyOverridesForCRUD
                        .Select(x => x == null ? null : UnmapStrategy(x))
                        .ToList(),
                    Children = r.Children?.Select(UnmapResourceClaim).ToList() ?? []
                })
                .ToList();

            _command.Execute(claimSetId, localResources);
        }

        private static ResourceClaim UnmapResourceClaim(Common.Infrastructure.ClaimSetEditor.ResourceClaim common)
        {
            return new ResourceClaim
            {
                Id = common.Id,
                Name = common.Name,
                ParentId = common.ParentId,
                ParentName = common.ParentName,
                IsParent = common.IsParent,
                Actions = common.Actions?.Select(a => new ResourceClaimAction
                {
                    Name = a.Name,
                    Enabled = a.Enabled
                }).ToList(),
                DefaultAuthorizationStrategiesForCRUD = common.DefaultAuthorizationStrategiesForCRUD
                    .Select(x => x == null ? null : UnmapStrategy(x))
                    .ToList(),
                AuthorizationStrategyOverridesForCRUD = common.AuthorizationStrategyOverridesForCRUD
                    .Select(x => x == null ? null : UnmapStrategy(x))
                    .ToList(),
                Children = common.Children?.Select(UnmapResourceClaim).ToList() ?? []
            };
        }

        private static ClaimSetResourceClaimActionAuthStrategies UnmapStrategy(Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies common)
        {
            return new ClaimSetResourceClaimActionAuthStrategies
            {
                ActionId = common.ActionId,
                ActionName = common.ActionName,
                AuthorizationStrategies = common.AuthorizationStrategies?.Select(a => new AuthorizationStrategy
                {
                    AuthStrategyId = a.AuthStrategyId,
                    AuthStrategyName = a.AuthStrategyName
                }).ToList()
            };
        }
    }
}

public interface IUpdateResourcesOnClaimSetModel
{
    int ClaimSetId { get; }
    List<ResourceClaim>? ResourceClaims { get; }
}

