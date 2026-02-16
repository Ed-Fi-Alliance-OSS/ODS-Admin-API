// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.ODSInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.Features.EducationOrganizations;

public class ReadEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/educationOrganizations", GetEducationOrganizations)
            .WithSummaryAndDescription(
                "Retrieves all education organizations grouped by ODS instance",
                "Returns all education organizations from all ODS instances in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder
            .MapGet(endpoints, "/educationOrganizations/{instanceId}", GetEducationOrganizationsByInstance)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific ODS instance",
                "Returns all education organizations for the specified ODS instance in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetEducationOrganizations(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [AsParameters] CommonQueryParams commonQueryParams)
    {
        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            instanceId: null);

        return Results.Ok(educationOrganizations);
    }

    public static async Task<IResult> GetEducationOrganizationsByInstance(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetOdsInstanceQuery getOdsInstanceQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int instanceId)
    {
        getOdsInstanceQuery.Execute(instanceId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            instanceId: instanceId);

        return Results.Ok(educationOrganizations);
    }
}
