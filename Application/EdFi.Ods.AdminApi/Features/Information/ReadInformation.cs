// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using V3Tenants = EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;

namespace EdFi.Ods.AdminApi.Features.Information;

public class ReadInformation : IFeature
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(ReadInformation));

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", GetInformation)
            .WithMetadata(new SwaggerOperationAttribute("Retrieve API informational metadata", null))
            .WithResponse<InformationResult>(200)
            .WithResponseCode(500, FeatureCommonConstants.InternalServerErrorResponseDescription)
            .WithTags("Information")
            .AllowAnonymous();
    }

    public static async Task<InformationResult> GetInformation(
        IOptions<AppSettings> options,
        HttpContext httpContext)
    {
        if (!Enum.TryParse<AdminApiMode>(options.Value.AdminApiMode, true, out var adminApiMode))
        {
            throw new InvalidOperationException($"Invalid adminApiMode: {options.Value.AdminApiMode}");
        }

        TenancyResult? tenancy = null;

        if (adminApiMode is AdminApiMode.V2 or AdminApiMode.V3)
        {
            var isMultiTenant = options.Value.MultiTenancy;
            List<string> tenantNames;

            if (isMultiTenant)
            {
                tenantNames = adminApiMode switch
                {
                    AdminApiMode.V2 => (await httpContext.RequestServices.GetRequiredService<ITenantsService>().GetTenantsAsync())
                        .Select(t => t.TenantName)
                        .ToList(),
                    AdminApiMode.V3 => (await httpContext.RequestServices.GetRequiredService<V3Tenants.ITenantsService>().GetTenantsAsync())
                        .Select(t => t.TenantName)
                        .ToList(),
                    _ => []
                };
            }
            else
            {
                tenantNames = [];
            }

            tenancy = new TenancyResult(isMultiTenant, tenantNames);
        }

        var forwardedProto = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var forwardedHost = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var scheme = forwardedProto ?? httpContext.Request.Scheme;
        var host = forwardedHost ?? httpContext.Request.Host.ToString();

        if (forwardedProto is not null || forwardedHost is not null)
        {
            _logger.DebugFormat(
                "Information endpoint resolved host '{0}://{1}' from X-Forwarded-* headers (raw request was '{2}://{3}')",
                scheme,
                host,
                httpContext.Request.Scheme,
                httpContext.Request.Host
            );
        }

        var baseUrl = $"{scheme}://{host}{httpContext.Request.PathBase}";

        InformationResult BuildResult(string version, string build, string specificationVersion, string appName, string informationalVersion, string swaggerDocName) =>
            new(version, build, specificationVersion, tenancy, appName, informationalVersion, new ApiUrlsResult($"{baseUrl}/swagger/{swaggerDocName}/swagger.json"));

        return adminApiMode switch
        {
            AdminApiMode.V1 => BuildResult(
                V1.Infrastructure.Helpers.ConstantsHelpers.Version,
                V1.Infrastructure.Helpers.ConstantsHelpers.Build,
                AdminApiVersions.V1.VersionPath,
                V1.Infrastructure.Helpers.ConstantsHelpers.ApplicationName,
                V1.Infrastructure.Helpers.ConstantsHelpers.InformationalVersion,
                AdminApiVersions.V1.ToString()),
            AdminApiMode.V2 => BuildResult(
                ConstantsHelpers.Version,
                ConstantsHelpers.Build,
                AdminApiVersions.V2.VersionPath,
                ConstantsHelpers.ApplicationName,
                ConstantsHelpers.InformationalVersion,
                AdminApiVersions.V2.ToString()),
            AdminApiMode.V3 => BuildResult(
                V3.Infrastructure.Helpers.ConstantsHelpers.Version,
                V3.Infrastructure.Helpers.ConstantsHelpers.Build,
                AdminApiVersions.V3.VersionPath,
                V3.Infrastructure.Helpers.ConstantsHelpers.ApplicationName,
                V3.Infrastructure.Helpers.ConstantsHelpers.InformationalVersion,
                AdminApiVersions.V3.ToString()),
            _ => throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}")
        };
    }
}
