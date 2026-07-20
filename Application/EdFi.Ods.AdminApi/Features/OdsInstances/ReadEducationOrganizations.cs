// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public class ReadEducationOrganizations : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/odsInstances/edOrgs", GetEducationOrganizations)
            .WithSummaryAndDescription(
                "Retrieves all education organizations grouped by ODS instance",
                "Returns all education organizations from all ODS instances in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder
            .MapGet(endpoints, "/odsInstances/{instanceId}/edOrgs", GetEducationOrganizationsByInstance)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific ODS instance",
                "Returns all education organizations for the specified ODS instance in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetEducationOrganizations(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetDbInstancesQuery getDbInstancesQuery,
        [AsParameters] CommonQueryParams commonQueryParams)
    {
        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            instanceId: null);

        MergeDbInstanceData(educationOrganizations, getDbInstancesQuery, includeUnlinked: true);

        return Results.Ok(educationOrganizations);
    }

    public static async Task<IResult> GetEducationOrganizationsByInstance(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetOdsInstanceQuery getOdsInstanceQuery,
        [FromServices] IGetDbInstancesQuery getDbInstancesQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int instanceId)
    {
        getOdsInstanceQuery.Execute(instanceId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            instanceId: instanceId);

        MergeDbInstanceData(educationOrganizations, getDbInstancesQuery, includeUnlinked: false);

        return Results.Ok(educationOrganizations);
    }

    private static void MergeDbInstanceData(
        List<OdsInstanceWithEducationOrganizationsModel> instances,
        IGetDbInstancesQuery getDbInstancesQuery,
        bool includeUnlinked)
    {
        var allDbInstances = getDbInstancesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

        var linkedById = allDbInstances
            .Where(d => d.OdsInstanceId is not null)
            .GroupBy(d => d.OdsInstanceId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

        foreach (var instance in instances)
        {
            if (linkedById.TryGetValue(instance.Id, out var dbInstance))
            {
                instance.DbInstanceId = dbInstance.Id;
                instance.Status = dbInstance.Status;
                instance.DatabaseTemplate = dbInstance.DatabaseTemplate;
                instance.DatabaseName = dbInstance.DatabaseName;
            }
            else
            {
                instance.Status = DbInstanceStatus.Created.ToString();
            }
        }

        if (includeUnlinked)
        {
            var negativeId = -1;
            foreach (var dbInstance in allDbInstances.Where(d => d.OdsInstanceId is null))
            {
                instances.Add(new OdsInstanceWithEducationOrganizationsModel
                {
                    Id = negativeId--,
                    DbInstanceId = dbInstance.Id,
                    Name = dbInstance.Name ?? string.Empty,
                    Status = dbInstance.Status,
                    DatabaseTemplate = dbInstance.DatabaseTemplate,
                    DatabaseName = dbInstance.DatabaseName,
                    EducationOrganizations = new()
                });
            }
        }
    }
}
