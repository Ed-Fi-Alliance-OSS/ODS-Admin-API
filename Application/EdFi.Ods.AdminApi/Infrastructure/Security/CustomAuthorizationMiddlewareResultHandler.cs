// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace EdFi.Ods.AdminApi.Infrastructure.Security;

/// <summary>
/// Custom authorization middleware result handler that returns 400 Bad Request
/// instead of 403 Forbidden when authorization assertion fails due to missing or invalid scope claims.
/// </summary>
public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly IAuthorizationMiddlewareResultHandler _defaultHandler;

    public CustomAuthorizationMiddlewareResultHandler()
    {
        // Use the default framework implementation
        _defaultHandler = new AuthorizationMiddlewareResultHandler();
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        // Check if authorization failed and is scope-related
        if ((authorizeResult.Challenged || authorizeResult.Forbidden)
            && IsScopeAuthorizationFailure(authorizeResult))
        {
            await HandleScopeAuthorizationFailure(context);        return;
        }

        // Use default handler for all other cases
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static bool IsScopeAuthorizationFailure(PolicyAuthorizationResult authorizeResult)
    {
        // Check if any requirements failed that are related to scope assertions
        // This includes both the default policy scope assertion and custom scope policies
        return authorizeResult.AuthorizationFailure?.FailedRequirements?
            .Any(requirement => requirement is AssertionRequirement) == true;
    }

    private static async Task HandleScopeAuthorizationFailure(HttpContext context)
    {
        context.Response.StatusCode = 400; // Bad Request instead of 403 Forbidden
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            title = "Bad Request",
            status = 400,
            detail = "The request is missing required scope claims or has invalid scope values. Please ensure your access token contains the appropriate scope permissions.",
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(
            problemDetails,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        await context.Response.WriteAsync(json);
    }
}
