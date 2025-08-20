// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;
using EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Documentation;

public class TagByResourceUrlFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var urlParts = context.ApiDescription.RelativePath?.Split("/") ?? Array.Empty<string>();

        if (urlParts.Length == 0)
            return;

        var isVersionPart = new Regex("(v)\\d+");
        var resourceName = isVersionPart.IsMatch(urlParts[0])
            ? urlParts[1] : urlParts[0];

        if (!string.IsNullOrWhiteSpace(resourceName))
            operation.Tags = new List<OpenApiTag> { new() { Name = resourceName.Trim('/').ToTitleCase() } };
    }
}
