// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.EducationOrganizations;

public class ReadEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/educationOrganizations", GetEducationOrganizations)
            .WithSummaryAndDescription(
                "Retrieves all education organizations",
                "Returns all education organizations from all ODS instances"
            )
            .WithRouteOptions(b => b.WithResponse<List<EducationOrganizationModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder
            .MapGet(endpoints, "/educationOrganizations/{instanceId}", GetEducationOrganizationsByInstance)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific ODS instance",
                "Returns all education organizations for the specified ODS instance identifier"
            )
            .WithRouteOptions(b => b.WithResponse<List<EducationOrganizationModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetEducationOrganizations(
        [FromServices] ILogger<ReadEducationOrganizations> logger)
    {

        // TODO: Implement service layer integration when available
        // For now, return empty list as placeholder
        var educationOrganizations = new List<EducationOrganizationModel>();

        return Results.Ok(educationOrganizations);
    }

    public static async Task<IResult> GetEducationOrganizationsByInstance(
        [FromServices] ILogger<ReadEducationOrganizations> logger,
        int instanceId)
    {
        // TODO: Implement service layer integration when available
        // For now, return empty list as placeholder
        var educationOrganizations = new List<EducationOrganizationModel>();

        return Results.Ok(educationOrganizations);
    }
}
