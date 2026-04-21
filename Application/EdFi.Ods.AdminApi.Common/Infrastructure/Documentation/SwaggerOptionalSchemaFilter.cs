// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;

[AttributeUsage(AttributeTargets.Property)]
public class SwaggerOptionalAttribute : Attribute
{
}

public class SwaggerOptionalSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var properties = context.Type.GetProperties();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(SwaggerOptionalAttribute));
            var propertyNameInCamelCasing = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            if (attribute != null)
            {
                schema.Required?.Remove(propertyNameInCamelCasing);
            }
            else
            {
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(propertyNameInCamelCasing);
            }
        }
    }
}

public class SwaggerSchemaRemoveRequiredFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var properties = context.Type.GetProperties();

        foreach (var property in properties)
        {
            var propertyNameInCamelCasing = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            schema.Required?.Remove(propertyNameInCamelCasing);
        }
    }
}
