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
    public const string DefaultRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    public static class Scopes
    {
        public static readonly ScopeDefinition AdminApiFullAccess = new("edfi_admin_api/full_access", "Full access to the Admin API");

        public static IEnumerable<ScopeDefinition> AllScopes =
        [
            AdminApiFullAccess
        ];
    }

    public class ScopeDefinition(string scope, string scopeDescription)
    {
        public string Scope { get; } = scope;
        public string ScopeDescription { get; } = scopeDescription;
    }
}
