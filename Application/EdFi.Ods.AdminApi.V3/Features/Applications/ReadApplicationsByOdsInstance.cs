// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.Applications;

public class ReadApplicationsByOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var url = "odsInstances/{id}/applications";

        AdminApiEndpointBuilder.MapGet(endpoints, url, GetOdsInstanceApplications)
            .WithSummary("Retrieves applications assigned to a specific ODS instance based on the resource identifier.")
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetOdsInstanceApplications(
        IGetApplicationsByOdsInstanceIdQuery getApplicationByOdsInstanceIdQuery,
        IGetOdsInstanceIdsByApplicationIdQuery getOdsInstanceIdsByApplicationIdQuery,
        int id)
    {
        var applicationEntities = getApplicationByOdsInstanceIdQuery.Execute(id);
        var odsInstanceIdsByApplicationId = getOdsInstanceIdsByApplicationIdQuery.Execute(applicationEntities.Select(a => a.ApplicationId));
        var odsInstanceApplications = ApplicationMapper.ToModelList(applicationEntities, odsInstanceIdsByApplicationId);
        return Task.FromResult(Results.Ok(odsInstanceApplications));
    }
}



