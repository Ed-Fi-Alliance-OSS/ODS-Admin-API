// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Infrastructure.JsonContractResolvers;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.V1.Features.ClaimSets;

public class ReadClaimSets : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/claimsets", GetClaimSets)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<List<ClaimSetModel>>(200))
            .BuildForVersions(AdminApiVersions.V1);

        AdminApiEndpointBuilder.MapGet(endpoints, "/claimsets/{id}", GetClaimSet)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ClaimSetDetailsModel>(200))
            .BuildForVersions(AdminApiVersions.V1);
    }

    internal Task<IResult> GetClaimSets(
        IGetAllClaimSetsQuery getClaimSetsQuery, IGetApplicationsByClaimSetIdQuery getApplications, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var claimSets = getClaimSetsQuery.Execute(commonQueryParams).ToList();
        var model = mapper.Map<List<ClaimSetModel>>(claimSets);
        foreach (var claimSet in model)
        {
            claimSet.ApplicationsCount = getApplications.ExecuteCount(claimSet.Id);
        }
        return Task.FromResult(AdminApiResponse<List<ClaimSetModel>>.Ok(model));
    }

    internal Task<IResult> GetClaimSet(IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications,
        IMapper mapper, int id)
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
        var claimSetData = mapper.Map<ClaimSetDetailsModel>(claimSet);
        claimSetData.ApplicationsCount = getApplications.ExecuteCount(id);
        claimSetData.ResourceClaims = mapper.Map<List<ResourceClaimModel>>(allResources.ToList());
        return Task.FromResult(AdminApiResponse<ClaimSetDetailsModel>.Ok(claimSetData,
            new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new ShouldSerializeContractResolver()
            }));
    }
}
