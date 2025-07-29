// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.Features.ClaimSets;

public class ExportClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/claimSets/{id}/export", GetClaimSet)
            .WithSummary("Exports a specific claimset by id")
            .WithRouteOptions(b => b.WithResponse<ClaimSetDetailsModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetClaimSet(IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications, IMapper mapper, int id)
    {
        var claimSet = getClaimSetByIdQuery.Execute(id);

        var allResources = getResourcesByClaimSetIdQuery.AllResources(id);
        var applications = getApplications.Execute(id);
        var claimSetData = mapper.Map<ClaimSetDetailsModel>(claimSet);
        if (applications != null)
        {
            claimSetData.Applications = mapper.Map<List<SimpleApplicationModel>>(applications);
        }
        if (allResources != null)
        {
            claimSetData.ResourceClaims = mapper.Map<List<ClaimSetResourceClaimModel>>(allResources.ToList());
        }

        return Task.FromResult(Results.Ok(claimSetData));
    }
}
