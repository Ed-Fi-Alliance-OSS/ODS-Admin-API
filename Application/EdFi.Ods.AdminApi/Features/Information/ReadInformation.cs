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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using V3Tenants = EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;

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

    public static async Task<InformationResult> GetInformation(IOptions<AppSettings> options, HttpContext httpContext)
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

        return adminApiMode switch
        {
            AdminApiMode.V1 => new InformationResult(V1.Infrastructure.Helpers.ConstantsHelpers.Version, V1.Infrastructure.Helpers.ConstantsHelpers.Build, "v1", tenancy),
            AdminApiMode.V2 => new InformationResult(ConstantsHelpers.Version, ConstantsHelpers.Build, "v2", tenancy),
            AdminApiMode.V3 => new InformationResult(V3.Infrastructure.Helpers.ConstantsHelpers.Version, V3.Infrastructure.Helpers.ConstantsHelpers.Build, "v3", tenancy),
            _ => throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}")
        };
    }
}
