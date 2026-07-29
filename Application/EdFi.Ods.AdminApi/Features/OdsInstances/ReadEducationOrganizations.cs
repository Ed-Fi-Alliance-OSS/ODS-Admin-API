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
            .MapGet(endpoints, "/odsInstances/{instanceId}/edOrgs", GetEducationOrganizationsByInstance)
            .WithSummaryAndDescription(
                "Retrieves education organizations for a specific ODS instance",
                "Returns all education organizations for the specified ODS instance in a nested structure"
            )
            .WithRouteOptions(b => b.WithResponse<List<OdsInstanceWithEducationOrganizationsModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> GetEducationOrganizationsByInstance(
        [FromServices] IGetEducationOrganizationsQuery getEducationOrganizationsQuery,
        [FromServices] IGetOdsInstanceQuery getOdsInstanceQuery,
        [FromServices] IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery,
        [AsParameters] CommonQueryParams commonQueryParams,
        int instanceId)
    {
        getOdsInstanceQuery.Execute(instanceId);

        var educationOrganizations = await getEducationOrganizationsQuery.ExecuteAsync(
            commonQueryParams,
            instanceId: instanceId);

        MergeOdsInstanceManageData(educationOrganizations, getOdsInstanceManagesQuery);
        return Results.Ok(educationOrganizations);
    }

    private static void MergeOdsInstanceManageData(
        List<OdsInstanceWithEducationOrganizationsModel> instances,
        IGetOdsInstanceManagesQuery getOdsInstanceManagesQuery)
    {
        var allOdsInstanceManages = getOdsInstanceManagesQuery.Execute(new CommonQueryParams(0, int.MaxValue), null, null);

        var linkedById = allOdsInstanceManages
            .Where(d => d.OdsInstanceId is not null)
            .GroupBy(d => d.OdsInstanceId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.LastModifiedDate ?? d.LastRefreshed).First());

        foreach (var instance in instances)
        {
            if (instance.Id is int instanceId && linkedById.TryGetValue(instanceId, out var odsInstanceManage))
            {
                instance.OdsInstanceManageId = odsInstanceManage.Id;
                instance.Status = odsInstanceManage.Status;
                instance.DatabaseTemplate = odsInstanceManage.DatabaseTemplate;
                instance.DatabaseName = odsInstanceManage.DatabaseName;
            }
            else
            {
                instance.Status = OdsInstanceManageStatus.Created.ToString();
            }
        }
    }
}
