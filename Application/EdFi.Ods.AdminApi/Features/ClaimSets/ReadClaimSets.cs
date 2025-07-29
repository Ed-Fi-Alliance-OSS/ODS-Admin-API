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
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Features.ClaimSets;

public class ReadClaimSets : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/claimSets", GetClaimSets)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponse<List<ClaimSetModel>>(200))
           .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/claimSets/{id}", GetClaimSet)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ClaimSetDetailsModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetClaimSets(
        IGetAllClaimSetsQuery getClaimSetsQuery, IGetApplicationsByClaimSetIdQuery getApplications, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var claimSets = mapper.Map<List<ClaimSetModel>>(getClaimSetsQuery.Execute(
            commonQueryParams,
            id,
            name));
        foreach (var claimSet in claimSets)
        {
            claimSet.Applications = mapper.Map<List<SimpleApplicationModel>>(getApplications.Execute(claimSet.Id));
        }
        return Task.FromResult(Results.Ok(claimSets));
    }

    internal static Task<IResult> GetClaimSet(IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications, IMapper mapper, int id)
    {
        ClaimSet claimSet;
        try
        {
            claimSet = getClaimSetByIdQuery.Execute(id);
        }
        catch (AdminApiException)
        {
            throw new NotFoundException<int>("claimset", id);
        }

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
