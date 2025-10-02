// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using EdFi.Ods.AdminApi.Common.Constants;
using AdminApiV1Features = EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;
using AdminApiV2Features = EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Infrastructure;

public static class WebApplicationExtensions
{
    public static void MapFeatureEndpoints(this WebApplication application)
    {
        var adminApiMode = application.Configuration.GetValue<AdminApiMode>("AppSettings:adminApiMode", AdminApiMode.V2);

        switch (adminApiMode)
        {
            case AdminApiMode.V1:
                foreach (var routeBuilder in AdminApiV1Features.AdminApiV1FeatureHelper.GetFeatures())
                {
                    routeBuilder.MapEndpoints(application);
                }
                new Features.Information.ReadInformation().MapEndpoints(application);
                break;

            case AdminApiMode.V2:
                foreach (var routeBuilder in AdminApiV2Features.AdminApiFeatureHelper.GetFeatures())
                {
                    routeBuilder.MapEndpoints(application);
                }
                break;

            default:
                throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}");
        }
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
