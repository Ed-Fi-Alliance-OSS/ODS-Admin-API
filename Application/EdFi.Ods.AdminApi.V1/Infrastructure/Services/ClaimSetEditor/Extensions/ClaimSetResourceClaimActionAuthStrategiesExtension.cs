// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor.Extensions;

public static class ClaimSetResourceClaimActionAuthStrategiesExtension
{
    public static ClaimSetResourceClaimActionAuthStrategies? Create(this ClaimSetResourceClaimActionAuthStrategies?[] claimSetResourceClaimActionAuthStrategies)
    {
        return claimSetResourceClaimActionAuthStrategies.Length > 0 ? claimSetResourceClaimActionAuthStrategies[0] : null;
    }

    public static ClaimSetResourceClaimActionAuthStrategies? Read(this ClaimSetResourceClaimActionAuthStrategies?[] claimSetResourceClaimActionAuthStrategies)
    {
        return claimSetResourceClaimActionAuthStrategies.Length > 1 ? claimSetResourceClaimActionAuthStrategies[1] : null;
    }

    public static ClaimSetResourceClaimActionAuthStrategies? Update(this ClaimSetResourceClaimActionAuthStrategies?[] claimSetResourceClaimActionAuthStrategies)
    {
        return claimSetResourceClaimActionAuthStrategies.Length > 2 ? claimSetResourceClaimActionAuthStrategies[2] : null;
    }

    public static ClaimSetResourceClaimActionAuthStrategies? Delete(this ClaimSetResourceClaimActionAuthStrategies?[] claimSetResourceClaimActionAuthStrategies)
    {
        return claimSetResourceClaimActionAuthStrategies.Length > 3 ? claimSetResourceClaimActionAuthStrategies[3] : null;
    }

    public static ClaimSetResourceClaimActionAuthStrategies? ReadChanges(this ClaimSetResourceClaimActionAuthStrategies?[] claimSetResourceClaimActionAuthStrategies)
    {
        return claimSetResourceClaimActionAuthStrategies.Length > 4 ? claimSetResourceClaimActionAuthStrategies[4] : null;
    }

    public static ClaimSetResourceClaimActionAuthStrategies?[] AddAuthorizationStrategyOverrides(this ClaimSetResourceClaimActionAuthStrategies?[] claimSetResourceClaimActionAuthStrategies,
        string actionName, AuthorizationStrategy? strategy)
    {
        if (strategy == null)
            return claimSetResourceClaimActionAuthStrategies;

        if (actionName == Action.Create.Value)
        {
            if (claimSetResourceClaimActionAuthStrategies[0] == null)
                claimSetResourceClaimActionAuthStrategies[0] = new ClaimSetResourceClaimActionAuthStrategies();
            claimSetResourceClaimActionAuthStrategies[0]!.AuthorizationStrategies.Add(strategy);
        }
        else if (actionName == Action.Read.Value)
        {
            if (claimSetResourceClaimActionAuthStrategies[1] == null)
                claimSetResourceClaimActionAuthStrategies[1] = new ClaimSetResourceClaimActionAuthStrategies();
            claimSetResourceClaimActionAuthStrategies[1]!.AuthorizationStrategies.Add(strategy);
        }
        else if (actionName == Action.Update.Value)
        {
            if (claimSetResourceClaimActionAuthStrategies[2] == null)
                claimSetResourceClaimActionAuthStrategies[2] = new ClaimSetResourceClaimActionAuthStrategies();
            claimSetResourceClaimActionAuthStrategies[2]!.AuthorizationStrategies.Add(strategy);
        }
        else if (actionName == Action.Delete.Value)
        {
            if (claimSetResourceClaimActionAuthStrategies[3] == null)
                claimSetResourceClaimActionAuthStrategies[3] = new ClaimSetResourceClaimActionAuthStrategies();
            claimSetResourceClaimActionAuthStrategies[3]!.AuthorizationStrategies.Add(strategy);
        }
        else if (actionName == Action.ReadChanges.Value && claimSetResourceClaimActionAuthStrategies.Length == 5)
        {
            if (claimSetResourceClaimActionAuthStrategies[4] == null)
                claimSetResourceClaimActionAuthStrategies[4] = new ClaimSetResourceClaimActionAuthStrategies();
            claimSetResourceClaimActionAuthStrategies[4]!.AuthorizationStrategies.Add(strategy);
        }

        return claimSetResourceClaimActionAuthStrategies;
    }
}
