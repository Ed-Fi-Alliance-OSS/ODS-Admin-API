// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor.Extensions;

public static class AuthorizationStrategiesExtension
{
    public static AuthorizationStrategy? Create(this AuthorizationStrategy?[] authorizationStrategies)
    {
        return authorizationStrategies.Length > 0 ? authorizationStrategies[0] : null;
    }

    public static AuthorizationStrategy? Read(this AuthorizationStrategy?[] authorizationStrategies)
    {
        return authorizationStrategies.Length > 1 ? authorizationStrategies[1] : null;
    }

    public static AuthorizationStrategy? Update(this AuthorizationStrategy?[] authorizationStrategies)
    {
        return authorizationStrategies.Length > 2 ? authorizationStrategies[2] : null;
    }

    public static AuthorizationStrategy? Delete(this AuthorizationStrategy?[] authorizationStrategies)
    {
        return authorizationStrategies.Length > 3 ? authorizationStrategies[3] : null;
    }

    public static AuthorizationStrategy? ReadChanges(this AuthorizationStrategy?[] authorizationStrategies)
    {
        return authorizationStrategies.Length > 4 ? authorizationStrategies[4] : null;
    }

    public static AuthorizationStrategy?[] AddAuthorizationStrategyOverrides(this AuthorizationStrategy?[] authorizationStrategies,
        string actionName, AuthorizationStrategy? strategy)
    {
        if (actionName == Action.Create.Value)
            authorizationStrategies[0] = strategy;
        else if (actionName == Action.Read.Value)
            authorizationStrategies[1] = strategy;
        else if (actionName == Action.Update.Value)
            authorizationStrategies[2] = strategy;
        else if (actionName == Action.Delete.Value)
            authorizationStrategies[3] = strategy;
        else if (actionName == Action.ReadChanges.Value)
            authorizationStrategies[4] = strategy;

        return authorizationStrategies;
    }
}
