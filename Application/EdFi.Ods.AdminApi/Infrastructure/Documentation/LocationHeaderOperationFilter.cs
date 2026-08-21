// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Documentation;

/// <summary>
/// Documents the "Location" header on 201 and 202 responses. Endpoints whose Location does not
/// point at the created resource (e.g. a queued job's status endpoint) can override the
/// description via <see cref="LocationHeaderDescriptionMetadata"/>.
/// </summary>
public class LocationHeaderOperationFilter : IOperationFilter
{
    private const string DefaultDescription = "URI of the resource that was created.";
    private static readonly string[] LocationHeaderStatusCodes = ["201", "202"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var statusCode = LocationHeaderStatusCodes.FirstOrDefault(operation.Responses.ContainsKey);
        if (statusCode is null)
            return;

        var response = operation.Responses[statusCode];

        var descriptionOverride = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<LocationHeaderDescriptionMetadata>()
            .FirstOrDefault()
            ?.Description;

        response.Headers ??= new Dictionary<string, OpenApiHeader>();
        response.Headers["Location"] = new OpenApiHeader
        {
            Description = descriptionOverride ?? DefaultDescription,
            Schema = new OpenApiSchema { Type = "string", Format = "uri" }
        };
    }
}
