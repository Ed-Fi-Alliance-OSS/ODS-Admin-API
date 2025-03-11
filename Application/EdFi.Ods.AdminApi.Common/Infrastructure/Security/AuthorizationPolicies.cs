// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security
{
    public static class AuthorizationPolicies
    {
        // Define the roles that are used in the application
        private static readonly IEnumerable<string> _adminApiClientRole = new List<string> { Roles.AdminApiClientRole.RoleName };
        private static readonly IEnumerable<string> _adminConsoleUserRole = new List<string> { Roles.AdminConsoleUserRole.RoleName };
        // Create policies by role
        public static readonly PolicyDefinition AdminApiClientPolicy = new PolicyDefinition("AdminApiClient", _adminApiClientRole);
        public static readonly PolicyDefinition AdminConsoleUserPolicy = new PolicyDefinition("AdminConsoleUserPolicy", _adminConsoleUserRole);

        public static readonly PolicyDefinition DefaultRolePolicy = AdminApiClientPolicy;
        public static readonly IEnumerable<PolicyDefinition> RolePolicies = new List<PolicyDefinition>
        {
            DefaultRolePolicy,
            AdminConsoleUserPolicy
        };
        // Create policies by scope
        public static readonly PolicyDefinition AdminApiFullAccessScopePolicy = new PolicyDefinition("AdminApiFullAccessScopePolicy", SecurityConstants.Scopes.AdminApiFullAccess.Scope);
        public static readonly PolicyDefinition AdminApiTenantAccessScopePolicy = new PolicyDefinition("AdminApiTenantAccessScopePolicy", SecurityConstants.Scopes.AdminApiTenantAccess.Scope);
        public static readonly PolicyDefinition AdminApiWorkerScopePolicy = new PolicyDefinition("AdminApiWorkerScopePolicy", SecurityConstants.Scopes.AdminApiWorker.Scope);
        public static readonly PolicyDefinition DefaultScopePolicy = AdminApiFullAccessScopePolicy;
        public static readonly IEnumerable<PolicyDefinition> ScopePolicies = new List<PolicyDefinition>
        {
            AdminApiFullAccessScopePolicy,
            AdminApiTenantAccessScopePolicy,
            AdminApiWorkerScopePolicy
        };
    }

    public class PolicyDefinition
    {
        public string PolicyName { get; }
        public IEnumerable<string> Roles { get; }
        public string Scope { get; }
        public RolesAuthorizationRequirement RolesAuthorizationRequirement { get; }

        public PolicyDefinition(string policyName, IEnumerable<string> roles)
        {
            PolicyName = policyName;
            Roles = roles;
            RolesAuthorizationRequirement = new RolesAuthorizationRequirement(roles);
            Scope = string.Empty;
        }

        public PolicyDefinition(string policyName, string scope)
        {
            PolicyName = policyName;
            Scope = scope;
            Roles = new List<string>();
            RolesAuthorizationRequirement = new RolesAuthorizationRequirement(Roles);
        }
        public override string ToString()
        {
            return this.PolicyName;
        }
    }
}
