// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class ReadEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/dataStores/edOrgs", GetEducationOrganizations)
            .WithSummaryAndDescription(
                "Retrieves all education organizations grouped by data store",
                "Returns all education organizations from all data stores in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<DataStoreWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder
            .MapGet(endpoints, "/dataStores/{dataStoreId}/edOrgs", GetEducationOrganizationsByDataStore)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific data store",
                "Returns all education organizations for the specified data store in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<DataStoreWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> GetEducationOrganizations(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [AsParameters] CommonQueryParams commonQueryParams)
    {
        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            dataStoreId: null);

        return Results.Ok(educationOrganizations);
    }

    public static async Task<IResult> GetEducationOrganizationsByDataStore(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetDataStoreQuery getDataStoreQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int dataStoreId)
    {
        getDataStoreQuery.Execute(dataStoreId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            dataStoreId: dataStoreId);

        return Results.Ok(educationOrganizations);
    }
}
