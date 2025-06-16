// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security;

public class ScopeValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ScopeValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Check if the response is 403 and if there's a scope validation failure marker
        if (context.Response.StatusCode == 403 &&
            context.Items.ContainsKey("ScopeValidationFailure"))
        {
            var scopeFailure = context.Items["ScopeValidationFailure"] as ScopeValidationFailure;
            if (scopeFailure != null)
            {
                // Convert to 400 Bad Request
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new
                {
                    title = "Validation failed",
                    errors = new Dictionary<string, List<string>>
                    {
                        ["Scope"] = new List<string>
                        {
                            $"Required scope '{scopeFailure.RequiredScope}' is missing from the token."
                        }
                    }
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
            }
        }
    }
}
