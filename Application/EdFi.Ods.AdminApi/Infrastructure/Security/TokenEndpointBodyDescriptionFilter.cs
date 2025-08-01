// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Security;

/// <summary>
/// Swashbuckle OperationFilter to manually specify the Request Body for the Token endpoint,
/// which pulls the request from HttpContext as per OpenIddict convention.
/// </summary>
public class TokenEndpointBodyDescriptionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor?.ControllerName != "Connect" || descriptor.ActionName != "Token")
            return;

        operation.RequestBody ??= new OpenApiRequestBody();
        operation.RequestBody.Content = new Dictionary<string, OpenApiMediaType>
        {
            { "application/x-www-form-urlencoded", BuildTokenRequestBodyDescription() }
        };
    }

    private static OpenApiMediaType BuildTokenRequestBodyDescription() => new()
    {
        Schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                { "client_id", new OpenApiSchema { Type = "string"} },
                { "client_secret", new OpenApiSchema { Type = "string"} },
                { "grant_type", new OpenApiSchema { Type = "string"} },
                { "scope", new OpenApiSchema { Type = "string"} },
            }
        }
    };
}
