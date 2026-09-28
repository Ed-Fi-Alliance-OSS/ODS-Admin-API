// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Documentation;

[AttributeUsage(AttributeTargets.Property)]
public class SwaggerOptionalAttribute : Attribute
{
}

public class SwaggerOptionalSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
            return;

        var properties = context.Type.GetProperties();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(SwaggerOptionalAttribute));
            var propertyNameInCamelCasing = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            if (attribute != null)
            {
                concreteSchema.Required?.Remove(propertyNameInCamelCasing);
            }
            else
            {
                if (concreteSchema.Required == null)
                {
                    concreteSchema.Required = new HashSet<string>() { propertyNameInCamelCasing };
                }
                else
                {
                    concreteSchema.Required.Add(propertyNameInCamelCasing);
                }
            }
        }
    }
}
