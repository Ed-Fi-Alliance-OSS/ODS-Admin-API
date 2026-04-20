// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Features.OdsInstanceContext;

public class ReadOdsInstanceContext : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstanceContexts", GetOdsInstanceContexts)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceContextModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstanceContexts/{id}", GetOdsInstanceContext)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceContextModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetOdsInstanceContexts(IGetOdsInstanceContextsQuery getOdsInstanceContextsQuery, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var odsInstanceContextList = OdsInstanceContextMapper.ToModelList(getOdsInstanceContextsQuery.Execute(commonQueryParams));
        return Task.FromResult(Results.Ok(odsInstanceContextList));
    }

    internal static Task<IResult> GetOdsInstanceContext(IGetOdsInstanceContextByIdQuery getOdsInstanceContextByIdQuery, int id)
    {
        var odsInstanceContext = getOdsInstanceContextByIdQuery.Execute(id);
        var model = OdsInstanceContextMapper.ToModel(odsInstanceContext);
        return Task.FromResult(Results.Ok(model));
    }
}



