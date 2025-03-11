// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security;

public static class SecurityConstants
{
    public const string ConnectRoute = "connect";
    public const string TokenActionName = "token";
    public const string TokenEndpoint = $"{ConnectRoute}/{TokenActionName}";
    public const string RegisterActionName = "register";

    public static class Scopes
    {
        public static readonly ScopeDefinition AdminApiFullAccess = new ScopeDefinition("edfi_admin_api/full_access", "Full access to the Admin API");
        public static readonly ScopeDefinition AdminApiTenantAccess = new ScopeDefinition("edfi_admin_api/tenant_access", "Access to a specific tenant");
        public static readonly ScopeDefinition AdminApiWorker = new ScopeDefinition("edfi_admin_api/worker", "Worker access to the Admin API");

        public static IEnumerable<ScopeDefinition> AllScopes = new List<ScopeDefinition>
        {
            AdminApiFullAccess,
            AdminApiTenantAccess,
            AdminApiWorker
        };
    }
    public class ScopeDefinition
    {
        public string Scope { get; }
        public string ScopeDescription { get; }
        public ScopeDefinition(string scope, string scopeDescription)
        {
            Scope = scope;
            ScopeDescription = scopeDescription;
        }
    }
}
