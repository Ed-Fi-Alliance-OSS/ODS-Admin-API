// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security;

public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        // Check if user has scope claim
        if (!context.User.HasClaim(c => c.Type == "scope"))
        {
            // Add a marker to HttpContext to indicate this is a scope failure
            if (context.Resource is HttpContext httpContext)
            {
                httpContext.Items["ScopeValidationFailure"] = new ScopeValidationFailure
                {
                    Reason = "Missing scope claim",
                    RequiredScope = requirement.RequiredScope
                };
            }
            context.Fail();
            return Task.CompletedTask;
        }

        // Get the scopes from the token
        var scopes = context.User.FindFirst(c => c.Type == "scope")?.Value
            .Split(' ')
            .ToList();

        // Check if user has the required scope or full access scope
        if (scopes != null && (scopes.Contains("adminapi:fullaccess", StringComparer.OrdinalIgnoreCase)
            || scopes.Contains(requirement.RequiredScope, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Add a marker to HttpContext to indicate this is a scope failure
        if (context.Resource is HttpContext httpContext2)
        {
            httpContext2.Items["ScopeValidationFailure"] = new ScopeValidationFailure
            {
                Reason = "Required scope missing",
                RequiredScope = requirement.RequiredScope
            };
        }
        context.Fail();
        return Task.CompletedTask;
    }
}

public class ScopeValidationFailure
{
    public string Reason { get; set; } = string.Empty;
    public string RequiredScope { get; set; } = string.Empty;
}
