// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Information;

public class ReadInformation : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", GetInformation)
            .WithMetadata(new SwaggerOperationAttribute("Retrieve API informational metadata", null))
            .WithResponse<InformationResult>(200)
            .WithResponseCode(500, FeatureCommonConstants.InternalServerErrorResponseDescription)
            .WithTags("Information")
            .AllowAnonymous();
    }

    public static Task<InformationResult> GetInformation(
        IOptions<AppSettings> options,
        HttpContext httpContext)
    {
        if (!Enum.TryParse<AdminApiMode>(options.Value.AdminApiMode, true, out var adminApiMode))
        {
            throw new InvalidOperationException($"Invalid adminApiMode: {options.Value.AdminApiMode}");
        }

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}";

        // Version/Build/ApplicationName/InformationalVersion are shared across V1/V2/V3 now that all
        // three ship together as one product release; only specificationVersion (and the swagger doc
        // it points at) varies per mode.
        InformationResult BuildResult(string specificationVersion, string? tenancyVersionPath) =>
            new(
                ApiInformationHelper.Version,
                ApiInformationHelper.Build,
                adminApiMode.ToString().ToLowerInvariant(),
                ApiInformationHelper.ApplicationName,
                ApiInformationHelper.InformationalVersion,
                new ApiUrlsResult(
                    $"{baseUrl}/swagger/{specificationVersion}/swagger.json",
                    tenancyVersionPath is null ? string.Empty : $"{baseUrl}/{tenancyVersionPath}/tenancy"));

        var result = adminApiMode switch
        {
            // V1 has no tenancy endpoint.
            AdminApiMode.V1 => BuildResult(AdminApiVersions.V1.ToString(), null),
            AdminApiMode.V2 => BuildResult(AdminApiVersions.V2.ToString(), AdminApiVersions.V2.VersionPath),
            AdminApiMode.V3 => BuildResult(AdminApiVersions.V3.ToString(), AdminApiVersions.V3.VersionPath),
            _ => throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}")
        };

        return Task.FromResult(result);
    }
}
