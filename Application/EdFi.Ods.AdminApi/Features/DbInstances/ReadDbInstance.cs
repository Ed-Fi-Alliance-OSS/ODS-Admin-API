// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.DbInstances;

public class ReadDbInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/dbinstances", GetDbInstances)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DbInstanceModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/dbinstances/{id}", GetDbInstance)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DbInstanceModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static Task<IResult> GetDbInstances(IGetDbInstancesQuery query, IMapper mapper,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var list = mapper.Map<List<DbInstanceModel>>(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetDbInstance(IGetDbInstanceByIdQuery query, IMapper mapper, int id)
    {
        var dbInstance = query.Execute(id);
        if (dbInstance == null)
        {
            throw new NotFoundException<int>("dbInstance", id);
        }
        var model = mapper.Map<DbInstanceModel>(dbInstance);
        return Task.FromResult(Results.Ok(model));
    }
}
