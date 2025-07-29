// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.ResourceClaimActions;

public class ReadResourceClaimActions : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/resourceClaimActions", GetResourceClaimsActions)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<List<ResourceClaimActionModel>>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetResourceClaimsActions(IGetResourceClaimActionsQuery getResourceClaimActionsQuery, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams, string? resourceName)
    {
        var resourceClaimActions = getResourceClaimActionsQuery.Execute(commonQueryParams, resourceName);
        return Task.FromResult(Results.Ok(resourceClaimActions));
    }
}
