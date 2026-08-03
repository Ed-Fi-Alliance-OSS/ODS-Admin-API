// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Documentation;

/// <summary>
/// Clears the document-wide OAuth security requirement on operations whose endpoint allows
/// anonymous access, so the generated spec doesn't imply a token is required to call them
/// (e.g. the token/register endpoints and the informational metadata endpoint).
/// </summary>
public class AnonymousOperationSecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            // Microsoft.OpenApi's V3 writer skips the "security" property entirely when the list
            // is empty (WriteOptionalCollection treats an empty collection the same as a missing
            // one), so a genuinely empty list can't be serialized. A single empty requirement
            // object ({}) is the OpenAPI-spec-legal equivalent: it overrides the document-level
            // requirement and is satisfied without any scheme, i.e. "no auth required" here.
            operation.Security = new List<OpenApiSecurityRequirement> { new OpenApiSecurityRequirement() };
        }
    }
}
