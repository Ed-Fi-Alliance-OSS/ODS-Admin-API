// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.Applications;

public class ReadApplicationsByDataStore : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var url = "dataStores/{id}/applications";

        AdminApiEndpointBuilder.MapGet(endpoints, url, GetDataStoreApplications)
            .WithSummary("Retrieves applications assigned to a specific data store based on the resource identifier.")
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetDataStoreApplications(
        IGetApplicationsByDataStoreIdQuery getApplicationsByDataStoreIdQuery,
        IGetOdsInstanceIdsByApplicationIdQuery getOdsInstanceIdsByApplicationIdQuery,
        int id)
    {
        var applicationEntities = getApplicationsByDataStoreIdQuery.Execute(id);
        var dataStoreIdsByApplicationId = getOdsInstanceIdsByApplicationIdQuery.Execute(applicationEntities.Select(a => a.ApplicationId));
        var applications = ApplicationMapper.ToModelList(applicationEntities, dataStoreIdsByApplicationId);
        return Task.FromResult(Results.Ok(applications));
    }
}
