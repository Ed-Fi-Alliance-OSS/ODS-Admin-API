// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Constants = EdFi.Ods.AdminApi.Common.Constants.Constants;

namespace EdFi.Ods.AdminApi.V3.Features.Tenants;

public class ReadTenants : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/tenants/{tenantName}/dataStores/edOrgs", GetTenantEdOrgsByDataStoresAsync)
            .WithSummaryAndDescription("Retrieves the data stores and their education organizations for a specific tenant", "Returns the tenant along with each of its data stores and the education organizations within them. In multi-tenant mode the tenant header must be supplied and must match the tenantName in the route. In single-tenant mode only the default tenant name is accepted.")
            .WithRouteOptions(b => b
                .WithResponse<TenantDetailsResponse>(200)
                .WithResponseCode(400, "Tenant header and URL tenant name do not match")
                .WithResponseCode(404, $"Not found: TenantName with ID {{tenantName}}. It may have been recently deleted."))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> GetTenantEdOrgsByDataStoresAsync(
        HttpRequest request,
        [FromServices] ITenantsService tenantsService,
        IGetDataStoresQuery getDataStoresQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IGetDataStoreManagesQuery getDataStoreManagesQuery,
        IMemoryCache memoryCache,
        IOptions<AppSettings> options,
        IOptions<SwaggerSettings> _swaggerOptions,
        string tenantName
    )
    {
        if (options.Value.MultiTenancy)
        {
            if (!IsRequestFromSwagger(request))
            {
                var tenantHeader = request.Headers["tenant"].FirstOrDefault();

                if (tenantHeader is null)
                    throw new ValidationException([new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_MissingHeader)]);

                if (!string.Equals(tenantName, tenantHeader, StringComparison.OrdinalIgnoreCase))
                    throw new ValidationException([new ValidationFailure("Tenant", ErrorMessagesConstants.Tenant_ParameterMismatch)]);
            }
        }
        else if (!string.Equals(tenantName, Constants.DefaultTenantName, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException<string>("TenantName", tenantName);
        }

        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(
            getDataStoresQuery, getEducationOrganizationQuery, getDataStoreManagesQuery, tenantName);

        if (tenant is null)
            return Results.NotFound();

        return Results.Ok(
            new TenantDetailsResponse
            {
                Id = tenant.TenantName,
                Name = tenant.TenantName,
                DataStores = tenant.DataStores
            }
        );
    }

    private static bool IsRequestFromSwagger(HttpRequest request)
    {
        return (request.Path.Value != null &&
            request.Path.Value.Contains("swagger", StringComparison.InvariantCultureIgnoreCase)) ||
            request.Headers.Referer.FirstOrDefault(x => x != null && x.ToLower().Contains("swagger", StringComparison.InvariantCultureIgnoreCase)) != null;

    }
}

public class TenantDetailsResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    [JsonPropertyName("dataStores")]
    public List<TenantDataStoreModel>? DataStores { get; set; }
}
