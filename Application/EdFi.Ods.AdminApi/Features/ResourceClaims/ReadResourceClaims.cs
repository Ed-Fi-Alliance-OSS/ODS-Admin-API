// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.ClaimSets;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Features.ResourceClaims;

public class ReadResourceClaims : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/resourceClaims", GetResourceClaims)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<List<ResourceClaimModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/resourceClaims/{id}", GetResourceClaim)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ResourceClaimModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetResourceClaims(IGetResourceClaimsQuery getResourceClaimsQuery, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var resourceClaims = mapper.Map<List<ResourceClaimModel>>(getResourceClaimsQuery.Execute(
            commonQueryParams,
            id, name));

        return Task.FromResult(Results.Ok(resourceClaims));
    }

    internal static Task<IResult> GetResourceClaim(IGetResourceClaimByResourceClaimIdQuery getResourceClaimByResourceClaimIdQuery, IMapper mapper, int id)
    {
        var resourceClaim = getResourceClaimByResourceClaimIdQuery.Execute(id);
        if (resourceClaim == null)
        {
            throw new NotFoundException<int>("resourceclaim", id);
        }
        var model = mapper.Map<ResourceClaimModel>(resourceClaim);
        return Task.FromResult(Results.Ok(model));
    }
}
