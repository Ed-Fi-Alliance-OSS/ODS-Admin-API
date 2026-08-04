// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Documentation;

/// <summary>
/// Documents the "application/problem+json" <see cref="ProblemDetails"/> body that the API
/// actually returns for every 4xx/5xx response (see V3RequestErrorMiddleware), for any error
/// response that doesn't already declare its own content schema.
/// </summary>
public class ProblemDetailsResponseOperationFilter : IOperationFilter
{
    private const string ProblemJsonContentType = "application/problem+json";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

        foreach (var (statusCode, response) in operation.Responses)
        {
            if (!IsErrorStatusCode(statusCode))
                continue;

            if (response.Content is { Count: > 0 })
                continue;

            response.Content = new Dictionary<string, OpenApiMediaType>
            {
                [ProblemJsonContentType] = new OpenApiMediaType { Schema = schema }
            };
        }
    }

    private static bool IsErrorStatusCode(string statusCode) =>
        statusCode.Length == 3 && statusCode[0] is '4' or '5';
}
