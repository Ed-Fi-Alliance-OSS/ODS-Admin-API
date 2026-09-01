// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Tenancy;

public class ReadTenancy : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/tenancy", GetTenancyAsync)
            .WithSummaryAndDescription("Retrieves the list of configured tenant names", "Returns the list of tenant names when multi-tenant mode is enabled; an empty list otherwise.")
            .WithRouteOptions(b => b.WithResponse<TenancyResult>(200))
            .AllowAnonymous()
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<TenancyResult> GetTenancyAsync(
        [FromServices] ITenantsService tenantsService,
        IOptions<AppSettings> options)
    {
        if (!options.Value.MultiTenancy)
        {
            return new TenancyResult([]);
        }

        var tenantNames = (await tenantsService.GetTenantsAsync()).Select(t => t.TenantName).ToList();

        if (tenantNames.Count == 0)
        {
            throw new AdminApiException(
                "MultiTenancy is enabled but no tenants are configured. Check the Tenants section of appsettings.")
            {
                StatusCode = HttpStatusCode.InternalServerError
            };
        }

        return new TenancyResult(tenantNames);
    }
}

[SwaggerSchema(Title = "Tenancy")]
public class TenancyResult
{
    public TenancyResult(List<string> tenants)
    {
        Tenants = tenants;
    }

    [SwaggerSchema("List of available tenant names", Nullable = false)]
    public List<string> Tenants { get; }
}
