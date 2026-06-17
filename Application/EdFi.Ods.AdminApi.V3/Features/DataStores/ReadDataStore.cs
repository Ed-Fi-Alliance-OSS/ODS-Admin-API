// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class ReadDataStore : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores", GetDataStores)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores/{id}", GetDataStore)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreDetailModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetDataStores(IGetDataStoresQuery getDataStoresQuery, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name, string? dataStoreType)
    {
        var dataStores = DataStoreMapper.ToModelList(getDataStoresQuery.Execute(commonQueryParams, id, name, dataStoreType));
        return Task.FromResult(Results.Ok(dataStores));
    }

    internal static Task<IResult> GetDataStore(IGetDataStoreQuery getDataStoreQuery, int id)
    {
        var dataStore = getDataStoreQuery.Execute(id);
        var model = DataStoreMapper.ToDetailModel(dataStore);
        return Task.FromResult(Results.Ok(model));
    }
}
