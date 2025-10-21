// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

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
        var dbAuthStrategies = _securityContext.AuthorizationStrategies;

        foreach (var claim in resourceClaims)
        {
            if (claim.DefaultAuthStrategiesForCRUD != null && claim.DefaultAuthStrategiesForCRUD.Any())
            {
                foreach (var claimSetResourceClaimActionAuthStrategyItem in claim.DefaultAuthStrategiesForCRUD.Where(x => x != null))
                {
                    if (claimSetResourceClaimActionAuthStrategyItem != null && claimSetResourceClaimActionAuthStrategyItem.AuthorizationStrategies != null)
                    {
                        foreach (var authorizationStrategyItem in claimSetResourceClaimActionAuthStrategyItem.AuthorizationStrategies)
                        {
                            if (authorizationStrategyItem is null) continue;

                            var authStrategy = dbAuthStrategies.AsEnumerable().SingleOrDefault(
                            x => x.AuthorizationStrategyName.Equals(
                                authorizationStrategyItem.AuthStrategyName, StringComparison.InvariantCultureIgnoreCase));

                            if (authStrategy != null)
                            {
                                authorizationStrategyItem.AuthStrategyId = authStrategy.AuthorizationStrategyId;
                                authorizationStrategyItem.DisplayName = authStrategy.DisplayName;
                            }
                        }
                    }

                    
                }
            }

            if (claim.AuthStrategyOverridesForCRUD != null && claim.AuthStrategyOverridesForCRUD.Any())
            {
                foreach (var authStrategyOverride in claim.AuthStrategyOverridesForCRUD.Where(x => x != null))
                {
                    if (authStrategyOverride != null && authStrategyOverride.AuthorizationStrategies != null)
                    {
                        foreach (var authorizationStrategyItem in authStrategyOverride.AuthorizationStrategies)
                        {
                            if (authorizationStrategyItem is null) continue;

                            var authStrategy = dbAuthStrategies.AsEnumerable().SingleOrDefault(
                                x => x.AuthorizationStrategyName.Equals(
                                    authorizationStrategyItem.AuthStrategyName,
                                    StringComparison.InvariantCultureIgnoreCase));

                            if (authStrategy != null)
                            {
                                authorizationStrategyItem.AuthStrategyId = authStrategy.AuthorizationStrategyId;
                                authorizationStrategyItem.DisplayName = authStrategy.DisplayName;
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
