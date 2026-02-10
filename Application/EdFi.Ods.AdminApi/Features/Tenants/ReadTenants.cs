// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Constants = EdFi.Ods.AdminApi.Common.Constants.Constants;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public class ReadTenants : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/tenants/{tenantName}/edOrgsByInstances", GetTenantEdOrgsByInstancesAsync)
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetTenantEdOrgsByInstancesAsync(
        HttpRequest request,
        [FromServices] ITenantsService tenantsService,
        IGetOdsInstancesQuery getOdsInstancesQuery,
        IGetEducationOrganizationQuery getEducationOrganizationQuery,
        IMapper mapper,
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

        var tenant = await tenantsService.GetTenantEdOrgsByInstancesAsync(getOdsInstancesQuery, getEducationOrganizationQuery, mapper, tenantName);

        if (tenant is null)
            return Results.NotFound();

        return Results.Ok(
            new TenantDetailsResponse
            {
                Id = tenant.TenantName,
                Name = tenant.TenantName,
                OdsInstances = tenant.OdsInstances
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
    public List<TenantOdsInstanceModel>? OdsInstances { get; set; }
}
