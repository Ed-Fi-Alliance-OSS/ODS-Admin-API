// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Features.ResourceClaimActionAuthStrategies
{
    public class ResourceClaimActionAuthStrategyModel
    {
        public int ResourceClaimId { get; set; }
        public string ResourceName { get; set; } = string.Empty;

        public string ClaimName { get; set; } = string.Empty;

        public IReadOnlyList<ActionWithAuthorizationStrategy> AuthorizationStrategiesForActions { get; set; } = new List<ActionWithAuthorizationStrategy>();
    }

    public class ActionWithAuthorizationStrategy
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public IReadOnlyList<AuthorizationStrategyModelForAction> AuthorizationStrategies { get; set; } = new List<AuthorizationStrategyModelForAction>();

    }

    public class AuthorizationStrategyModelForAction
    {
        public int AuthStrategyId { get; set; }
        public string AuthStrategyName { get; set; } = string.Empty;
    }
}
