// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public interface IAuthStrategyResolver
{
    IEnumerable<ResourceClaim> ResolveAuthStrategies(IEnumerable<ResourceClaim> resourceClaims);
}

public class AuthStrategyResolver : IAuthStrategyResolver
{
    private readonly ISecurityContext _securityContext;

    public AuthStrategyResolver(ISecurityContext securityContext)
    {
        _securityContext = securityContext;
    }

    public IEnumerable<ResourceClaim> ResolveAuthStrategies(IEnumerable<ResourceClaim> resourceClaims)
    {
        var dbAuthStrategies = _securityContext.AuthorizationStrategies.ToList();

        foreach (var claim in resourceClaims)
        {
            if (claim.AuthorizationStrategyOverridesForCRUD != null && claim.AuthorizationStrategyOverridesForCRUD.Any())
            {
                foreach (var authStrategyOverride in claim.AuthorizationStrategyOverridesForCRUD.Where(x => x != null))
                {
                    if (authStrategyOverride is null) continue;
                    if (authStrategyOverride.AuthorizationStrategies != null)
                    {
                        foreach (var strategy in authStrategyOverride.AuthorizationStrategies)
                        {
                            var authStrategy = dbAuthStrategies.SingleOrDefault(
                            x => x.AuthorizationStrategyName.Equals(
                                strategy.AuthStrategyName,
                                StringComparison.InvariantCultureIgnoreCase));

                            if (authStrategy != null)
                            {
                                strategy.AuthStrategyId = authStrategy.AuthorizationStrategyId;
                                strategy.AuthStrategyName = authStrategy.AuthorizationStrategyName;
                            }
                        }
                    }
                }
            }

            if (claim.Children?.Any() ?? false)
            {
                claim.Children = ResolveAuthStrategies(claim.Children).ToList();
            }

            yield return claim;
        }
    }
}

