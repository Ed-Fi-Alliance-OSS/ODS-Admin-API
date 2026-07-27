// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
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
            .MapGet(endpoints, "/dataStores/{dataStoreId}/edOrgs", GetEducationOrganizationsByDataStore)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific data store",
                "Returns all education organizations for the specified data store in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<DataStoreWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> GetEducationOrganizationsByDataStore(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetDataStoreQuery getDataStoreQuery,
        [FromServices] IGetDbDataStoresQuery getDbDataStoresQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int dataStoreId)
    {
        getDataStoreQuery.Execute(dataStoreId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            dataStoreId: dataStoreId);

        MergeDbDataStoreData(educationOrganizations, getDbDataStoresQuery);
        return Results.Ok(educationOrganizations);
    }

    private static void MergeDbDataStoreData(
        List<DataStoreWithEducationOrganizationsModel> instances,
        IGetDbDataStoresQuery getDbDataStoresQuery)
    {
        var allDbDataStores = getDbDataStoresQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

        var linkedById = allDbDataStores
            .Where(d => d.OdsInstanceId is not null)
            .GroupBy(d => d.OdsInstanceId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

        foreach (var instance in instances)
        {
            if (instance.Id is int dataStoreId && linkedById.TryGetValue(dataStoreId, out var dbDataStore))
            {
                instance.Status = dbDataStore.Status;
                instance.DatabaseTemplate = dbDataStore.DatabaseTemplate;
                instance.DatabaseName = dbDataStore.DatabaseName;
            }
            else
            {
                instance.Status = DbInstanceStatus.Created.ToString();
            }
        }
    }
}
