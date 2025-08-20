// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public static class WebApplicationExtensions
{
    public static void MapFeatureEndpoints(this WebApplication application)
    {
        application.UseEndpoints(endpoints =>
        {
            foreach (var routeBuilder in Helpers.FeaturesHelper.GetFeatures())
            {
                routeBuilder.MapEndpoints(endpoints);
            }
        });
    }

    public static void DefineSwaggerUIWithApiVersions(this WebApplication application, params string[] versions)
    {
        application.UseSwaggerUI(definitions =>
        {
            definitions.RoutePrefix = "swagger";
            foreach (var version in versions)
            {
                definitions.SwaggerEndpoint($"{version}/swagger.json", version);
            }
        });
    }
}
