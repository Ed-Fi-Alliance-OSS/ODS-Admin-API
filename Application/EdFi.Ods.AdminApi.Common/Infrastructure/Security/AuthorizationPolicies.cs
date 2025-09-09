// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security
{
    public static class AuthorizationPolicies
    {
        // Create policies by scope
        public static readonly PolicyDefinition AdminApiFullAccessScopePolicy = new PolicyDefinition("AdminApiFullAccessScopePolicy", SecurityConstants.Scopes.AdminApiFullAccess.Scope);
        public static readonly PolicyDefinition DefaultScopePolicy = AdminApiFullAccessScopePolicy;
        public static readonly IEnumerable<PolicyDefinition> ScopePolicies = new List<PolicyDefinition>
        {
            AdminApiFullAccessScopePolicy,
        };
    }

    public class PolicyDefinition
    {
        public string PolicyName { get; }
        public string Scope { get; }

        public PolicyDefinition(string policyName, string scope)
        {
            PolicyName = policyName;
            Scope = scope;
        }
        public override string ToString()
        {
            return this.PolicyName;
        }
    }
}
