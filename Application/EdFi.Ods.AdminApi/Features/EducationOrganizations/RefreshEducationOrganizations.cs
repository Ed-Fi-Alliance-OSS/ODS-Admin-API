// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.EducationOrganizations;

public class RefreshEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/educationOrganizations/refresh", RefreshAllEducationOrganizations)
            .WithSummaryAndDescription(
                "Refreshes education organizations for all ODS instances",
                "Triggers a refresh of education organization data from all ODS instances"
            )
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder
            .MapPost(endpoints, "/educationOrganizations/refresh/{tenantName}", RefreshEducationOrganizationsByTenant)
            .WithSummaryAndDescription(
                "Refreshes education organizations for a specific tenant",
                "Triggers a refresh of education organization data for the specified tenant"
            )
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> RefreshAllEducationOrganizations(
        [FromServices] IEducationOrganizationService educationOrganizationService,
        [FromServices] ILogger<RefreshEducationOrganizations> logger)
    {
        try
        {
            await educationOrganizationService.Execute(null);
            logger.LogInformation("Successfully triggered refresh for all education organizations");
            return Results.Accepted();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing education organizations");
            return Results.Problem(
                title: "Error refreshing education organizations",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }

    public static async Task<IResult> RefreshEducationOrganizationsByTenant(
        [FromServices] IEducationOrganizationService educationOrganizationService,
        [FromServices] ILogger<RefreshEducationOrganizations> logger,
        string tenantName)
    {
        try
        {
            await educationOrganizationService.Execute(tenantName);
            logger.LogInformation("Successfully triggered refresh for tenant: {TenantName}", tenantName);
            return Results.Accepted();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing education organizations for tenant: {TenantName}", tenantName);
            return Results.Problem(
                title: $"Error refreshing education organizations for tenant: {tenantName}",
                detail: ex.Message,
                statusCode: 500
            );
        }
    }
}
