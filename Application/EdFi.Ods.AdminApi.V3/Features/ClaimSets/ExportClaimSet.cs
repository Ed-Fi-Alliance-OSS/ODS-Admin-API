// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V3.Features.ClaimSets;

public class ExportClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/claimSets/{id}/export", GetClaimSet)
            .WithSummary("Exports a specific claimset by id")
            .WithRouteOptions(b => b.WithResponse<ClaimSetDetailsModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetClaimSet(IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications, int id)
    {
        var claimSet = getClaimSetByIdQuery.Execute(id);

        var allResources = getResourcesByClaimSetIdQuery.AllResources(id);
        var applications = getApplications.Execute(id);
        var claimSetData = ClaimSetMapper.ToDetailsModel(claimSet);
        if (applications != null)
        {
            claimSetData.Applications = ClaimSetMapper.ToSimpleApplicationModelList(applications);
        }
        if (allResources != null)
        {
            claimSetData.ResourceClaims = ClaimSetMapper.ToClaimSetResourceClaimModelList(allResources.ToList());
        }

        return Task.FromResult(Results.Ok(claimSetData));
    }
}
