// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
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

        var requestBody = operation.RequestBody as OpenApiRequestBody ?? new OpenApiRequestBody();
        requestBody.Content = new Dictionary<string, OpenApiMediaType>
        {
            { "application/x-www-form-urlencoded", BuildTokenRequestBodyDescription() }
        };
        operation.RequestBody = requestBody;
    }

    private static OpenApiMediaType BuildTokenRequestBodyDescription() => new()
    {
        Schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                { "client_id", new OpenApiSchema { Type = JsonSchemaType.String } },
                { "client_secret", new OpenApiSchema { Type = JsonSchemaType.String } },
                { "grant_type", new OpenApiSchema { Type = JsonSchemaType.String } },
                { "scope", new OpenApiSchema { Type = JsonSchemaType.String } },
            }
        }
    };
}
