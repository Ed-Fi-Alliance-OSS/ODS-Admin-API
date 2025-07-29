// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.Applications;

public class ReadApplicationsByVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var url = "vendors/{id}/applications";

        AdminApiEndpointBuilder.MapGet(endpoints, url, GetVendorApplications)
            .WithSummary("Retrieves applications assigned to a specific vendor based on the resource identifier.")
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetVendorApplications(GetApplicationsByVendorIdQuery getApplicationByVendorIdQuery, IMapper mapper, int id)
    {
        var vendorApplications = mapper.Map<List<ApplicationModel>>(getApplicationByVendorIdQuery.Execute(id));
        return Task.FromResult(Results.Ok(vendorApplications));
    }
}
