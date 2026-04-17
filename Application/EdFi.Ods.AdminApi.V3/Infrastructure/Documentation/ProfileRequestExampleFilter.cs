// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;

[AttributeUsage(AttributeTargets.Method)]
public class ProfileRequestExampleAttribute : Attribute
{
}

public class ProfileRequestExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attribute = context.MethodInfo.GetCustomAttributes(typeof(ProfileRequestExampleAttribute), false).FirstOrDefault();
        if (attribute == null)
        {
            return;
        }

        var profileDefinition = @"<Profile name=""Test-Profile""><Resource name=""Resource1""><ReadContentType memberSelection=""IncludeOnly""><Collection name=""Collection1"" memberSelection=""IncludeOnly"">" +
                @"<Property name=""Property1"" /><Property name=""Property2"" /></Collection></ReadContentType><WriteContentType memberSelection=""IncludeOnly"">" +
                @"<Collection name=""Collection2"" memberSelection=""IncludeOnly""><Property name=""Property1"" /><Property name=""Property2"" />" +
                @"</Collection></WriteContentType></Resource></Profile>";

        var profileRequest = new
        {
            name = "Test-Profile",
            definition = profileDefinition
        };

        foreach (var schema in context.SchemaRepository.Schemas)
        {
            if (schema.Key.ToLower().Contains("addprofilerequest") || schema.Key.ToLower().Contains("editprofilerequest"))
            {
                schema.Value.Example = new OpenApiString(JsonConvert.SerializeObject(profileRequest, Formatting.Indented), true);
            }
        }
    }
}
