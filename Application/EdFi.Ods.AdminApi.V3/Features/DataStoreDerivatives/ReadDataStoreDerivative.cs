// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

public class ReadDataStoreDerivative : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreDerivatives", GetDataStoreDerivatives)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreDerivativeModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreDerivatives/{id}", GetDataStoreDerivative)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreDerivativeModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetDataStoreDerivatives(IGetDataStoreDerivativesQuery getDataStoreDerivativesQuery, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var list = DataStoreDerivativeMapper.ToModelList(getDataStoreDerivativesQuery.Execute(commonQueryParams));
        return Task.FromResult(Results.Ok(list));
    }

    internal static Task<IResult> GetDataStoreDerivative(IGetDataStoreDerivativeByIdQuery getDataStoreDerivativeByIdQuery, int id)
    {
        var item = getDataStoreDerivativeByIdQuery.Execute(id);
        var model = DataStoreDerivativeMapper.ToModel(item);
        return Task.FromResult(Results.Ok(model));
    }
}
