// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V1.Features.Applications;

public class ReadApplicationsByVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var url = "vendors/{id}/applications";

        AdminApiEndpointBuilder.MapGet(endpoints, url, GetVendorApplications)
            .WithSummaryAndDescription("Retrieves applications assigned to a specific vendor based on the resource identifier.", "Retrieves applications assigned to a specific vendor based on the resource identifier.")
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V1);
    }

    internal Task<IResult> GetVendorApplications(GetApplicationsByVendorIdQuery getApplicationByVendorIdQuery, IMapper mapper, int id)
    {
        var vendorApplications = mapper.Map<List<ApplicationModel>>(getApplicationByVendorIdQuery.Execute(id));
        return Task.FromResult(AdminApiResponse<List<ApplicationModel>>.Ok(vendorApplications));
    }
}
