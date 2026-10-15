// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Documentation;

/// <summary>
/// Adds the "application/json" example declared with <see cref="ResponseExampleMetadata"/>
/// to the matching response.
/// </summary>
public class ResponseExampleOperationFilter : IOperationFilter
{
    private const string JsonContentType = "application/json";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses is null)
            return;

        var examples = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<ResponseExampleMetadata>();

        foreach (var example in examples)
        {
            if (!operation.Responses.TryGetValue(example.StatusCode.ToString(), out var openApiResponse)
                || openApiResponse is not OpenApiResponse response
                || response.Content is null
                || !response.Content.TryGetValue(JsonContentType, out var mediaType))
            {
                continue;
            }

            mediaType.Example = JsonNode.Parse(example.ExampleJson);
        }
    }
}
