// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [FromServices] AdminApiDbContext context,
        [FromServices] IUsersContext usersContext)
    {
        var educationOrganizations = await context.EducationOrganizations
            .Join(
                usersContext.OdsInstances,
                edOrg => edOrg.InstanceId,
                instance => instance.OdsInstanceId,
                (edOrg, instance) => new EducationOrganizationModel
                {
                    EducationOrganizationId = edOrg.EducationOrganizationId,
                    NameOfInstitution = edOrg.NameOfInstitution,
                    ShortNameOfInstitution = edOrg.ShortNameOfInstitution,
                    Discriminator = edOrg.Discriminator,
                    ParentId = edOrg.ParentId,
                    InstanceId = edOrg.InstanceId,
                    InstanceName = instance.Name,
                    OdsDatabaseName = edOrg.OdsDatabaseName,
                    LastRefreshed = edOrg.LastRefreshed,
                    LastModifiedDate = edOrg.LastModifiedDate
                })
            .ToListAsync();

        return Results.Ok(educationOrganizations);
    }

    public static async Task<IResult> GetEducationOrganizationsByInstance(
        [FromServices] AdminApiDbContext context,
        [FromServices] IUsersContext usersContext,
        int instanceId)
    {
        var educationOrganizations = await context.EducationOrganizations
            .Where(e => e.InstanceId == instanceId)
            .Join(
                usersContext.OdsInstances,
                edOrg => edOrg.InstanceId,
                instance => instance.OdsInstanceId,
                (edOrg, instance) => new EducationOrganizationModel
                {
                    EducationOrganizationId = edOrg.EducationOrganizationId,
                    NameOfInstitution = edOrg.NameOfInstitution,
                    ShortNameOfInstitution = edOrg.ShortNameOfInstitution,
                    Discriminator = edOrg.Discriminator,
                    ParentId = edOrg.ParentId,
                    InstanceId = edOrg.InstanceId,
                    InstanceName = instance.Name,
                    OdsDatabaseName = edOrg.OdsDatabaseName,
                    LastRefreshed = edOrg.LastRefreshed,
                    LastModifiedDate = edOrg.LastModifiedDate
                })
            .ToListAsync();

        return Results.Ok(educationOrganizations);
    }
}
