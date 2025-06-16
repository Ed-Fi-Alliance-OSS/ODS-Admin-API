// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;

namespace EdFi.Ods.AdminApi.Infrastructure.Security;

/// <summary>
/// Helper class for creating scope-based authorization policies that throw
/// ScopeAuthorizationException (400 Bad Request) instead of returning 403 Forbidden.
/// </summary>
public static class ScopeAuthorizationHelper
{
    /// <summary>
    /// Creates a scope assertion that throws ScopeAuthorizationException when scope validation fails.
    /// This results in a 400 Bad Request instead of 403 Forbidden.
    /// </summary>
    public static Func<AuthorizationHandlerContext, bool> CreateScopeAssertion(string requiredScope)
    {
        return context =>
        {
            // Check if user has any scope claims
            if (!context.User.HasClaim(c => c.Type == OpenIddictConstants.Claims.Scope))
            {
                throw new ScopeAuthorizationException(
                    "Access token is missing scope claims. Please ensure your token contains the required scope permissions.");
            }

            var scopes = context.User.FindFirst(c => c.Type == OpenIddictConstants.Claims.Scope)?.Value
                .Split(' ')
                .ToList();

            if (scopes == null || scopes.Count == 0)
            {
                throw new ScopeAuthorizationException(
                    "Access token contains empty scope claims. Please ensure your token contains the required scope permissions.");
            }

            // Check for AdminApiFullAccess scope (global scope) or the specific required scope
            var hasRequiredScope = scopes.Contains(SecurityConstants.Scopes.AdminApiFullAccess.Scope, StringComparer.OrdinalIgnoreCase)
                                || scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase);

            if (!hasRequiredScope)
            {
                throw new ScopeAuthorizationException(
                    $"Access token does not contain the required scope '{requiredScope}'. " +
                    $"Available scopes: {string.Join(", ", scopes)}. " +
                    "Please ensure your token contains the appropriate scope permissions.");
            }

            return true;
        };
    }

    /// <summary>
    /// Creates a default scope assertion for the default policy that throws ScopeAuthorizationException when validation fails.
    /// </summary>
    public static Func<AuthorizationHandlerContext, bool> CreateDefaultScopeAssertion()
    {
        return context =>
        {
            // For the default policy, we want to reject if context has already succeeded
            // and check for the default scope
            if (context.HasSucceeded)
            {
                return false;
            }

            // Check if user has any scope claims
            if (!context.User.HasClaim(c => c.Type == OpenIddictConstants.Claims.Scope))
            {
                throw new ScopeAuthorizationException(
                    "Access token is missing scope claims. Please ensure your token contains the required scope permissions.");
            }

            var scopes = context.User.FindFirst(c => c.Type == OpenIddictConstants.Claims.Scope)?.Value
                .Split(' ')
                .ToList();

            if (scopes == null || scopes.Count == 0)
            {
                throw new ScopeAuthorizationException(
                    "Access token contains empty scope claims. Please ensure your token contains the required scope permissions.");
            }

            var hasDefaultScope = scopes.Exists(scopeValue =>
                string.Equals(scopeValue, AuthorizationPolicies.DefaultScopePolicy.Scope, StringComparison.OrdinalIgnoreCase));

            if (!hasDefaultScope)
            {
                throw new ScopeAuthorizationException(
                    $"Access token does not contain the required default scope '{AuthorizationPolicies.DefaultScopePolicy.Scope}'. " +
                    $"Available scopes: {string.Join(", ", scopes)}. " +
                    "Please ensure your token contains the appropriate scope permissions.");
            }

            return true;
        };
    }
}
