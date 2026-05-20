// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

public class ReadDataStoreContext : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreContexts", GetDataStoreContexts)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreContextModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreContexts/{id}", GetDataStoreContext)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreContextModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetDataStoreContexts(IGetDataStoreContextsQuery getDataStoreContextsQuery, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var list = DataStoreContextMapper.ToModelList(getDataStoreContextsQuery.Execute(commonQueryParams));
        return Task.FromResult(Results.Ok(list));
    }

    internal static Task<IResult> GetDataStoreContext(IGetDataStoreContextByIdQuery getDataStoreContextByIdQuery, int id)
    {
        var item = getDataStoreContextByIdQuery.Execute(id);
        var model = DataStoreContextMapper.ToModel(item);
        return Task.FromResult(Results.Ok(model));
    }
}
