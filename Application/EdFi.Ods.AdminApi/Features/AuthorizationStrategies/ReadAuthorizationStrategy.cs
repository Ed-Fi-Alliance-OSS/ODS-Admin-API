// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Features.AuthorizationStrategies;

public class ReadAuthorizationStrategy : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/authorizationStrategies", GetAuthStrategies)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<AuthorizationStrategyModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetAuthStrategies(IGetAuthStrategiesQuery getAuthStrategiesQuery, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var authStrategyList = mapper.Map<List<AuthorizationStrategyModel>>(getAuthStrategiesQuery.Execute(commonQueryParams));
        return Task.FromResult(Results.Ok(authStrategyList));
    }
}
