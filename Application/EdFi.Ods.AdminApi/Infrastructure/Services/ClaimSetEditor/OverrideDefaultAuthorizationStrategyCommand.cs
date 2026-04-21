// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public class OverrideDefaultAuthorizationStrategyCommand(ISecurityContext context) : OverrideDefaultAuthorizationStrategyCommandBase(context)
{
    public void Execute(IOverrideDefaultAuthorizationStrategyModel model)
    {
        ExecuteCore(WrapToCommonModel(model));
    }

    public void ExecuteOnSpecificAction(OverrideAuthStrategyOnClaimSetModel model)
    {
        ExecuteOnSpecificActionCore(model);
    }

    public void ResetAuthorizationStrategyOverrides(OverrideAuthStrategyOnClaimSetModel model)
    {
        ResetAuthorizationStrategyOverridesCore(model);
    }

    private static Common.Infrastructure.ClaimSetEditor.IOverrideDefaultAuthorizationStrategyModel WrapToCommonModel(IOverrideDefaultAuthorizationStrategyModel model)
    {
        return new LocalToCommonWrapper(model);
    }

    private sealed class LocalToCommonWrapper : Common.Infrastructure.ClaimSetEditor.IOverrideDefaultAuthorizationStrategyModel
    {
        private readonly IOverrideDefaultAuthorizationStrategyModel _local;

        public LocalToCommonWrapper(IOverrideDefaultAuthorizationStrategyModel local)
        {
            _local = local;
        }

        public int ClaimSetId => _local.ClaimSetId;
        public int ResourceClaimId => _local.ResourceClaimId;
        public List<Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides => GetMappedStrategies();

        private List<Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies?>? GetMappedStrategies()
        {
            if (_local.ClaimSetResourceClaimActionAuthStrategyOverrides == null)
                return null;

            return _local.ClaimSetResourceClaimActionAuthStrategyOverrides
                .Select(x => x == null ? null : WrapStrategy(x))
                .ToList();
        }

        private static Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies WrapStrategy(ClaimSetResourceClaimActionAuthStrategies local)
        {
            return new Common.Infrastructure.ClaimSetEditor.ClaimSetResourceClaimActionAuthStrategies
            {
                ActionId = local.ActionId,
                ActionName = local.ActionName,
                AuthorizationStrategies = local.AuthorizationStrategies?.Where(x => x != null).Select(x => new Common.Infrastructure.ClaimSetEditor.AuthorizationStrategy
                {
                    AuthStrategyId = x!.AuthStrategyId,
                    AuthStrategyName = x!.AuthStrategyName
                }).ToList()
            };
        }
    }
}

public interface IOverrideDefaultAuthorizationStrategyModel
{
    int ClaimSetId { get; }
    int ResourceClaimId { get; }
    List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides { get; }
}

public class OverrideAuthStrategyOnClaimSetModel : Common.Infrastructure.ClaimSetEditor.OverrideAuthStrategyOnClaimSetModel
{
}

public class OverrideAuthorizationStrategyModel : IOverrideDefaultAuthorizationStrategyModel
{
    public int ClaimSetId { get; set; }
    public int ResourceClaimId { get; set; }
    public List<ClaimSetResourceClaimActionAuthStrategies?>? ClaimSetResourceClaimActionAuthStrategyOverrides { get; set; }
}

