// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.OdsInstanceDerivative;

public class ReadOdsInstanceDerivative : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstanceDerivatives", GetOdsInstanceDerivatives)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceDerivativeModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstanceDerivatives/{id}", GetOdsInstanceDerivative)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceDerivativeModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetOdsInstanceDerivatives(IGetOdsInstanceDerivativesQuery getOdsInstanceDerivativesQuery, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var odsInstanceDerivativeList = OdsInstanceDerivativeMapper.ToModelList(getOdsInstanceDerivativesQuery.Execute(commonQueryParams));
        return Task.FromResult(Results.Ok(odsInstanceDerivativeList));
    }

    internal static Task<IResult> GetOdsInstanceDerivative(IGetOdsInstanceDerivativeByIdQuery getOdsInstanceDerivativeByIdQuery, int id)
    {
        var odsInstanceDerivative = getOdsInstanceDerivativeByIdQuery.Execute(id);
        var model = OdsInstanceDerivativeMapper.ToModel(odsInstanceDerivative);
        return Task.FromResult(Results.Ok(model));
    }
}
