// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.OdsInstanceContext;

public class ReadOdsInstanceContext : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstanceContexts", GetOdsInstanceContexts)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceContextModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstanceContexts/{id}", GetOdsInstanceContext)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceContextModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetOdsInstanceContexts(IGetOdsInstanceContextsQuery getOdsInstanceContextsQuery, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var odsInstanceContextList = mapper.Map<List<OdsInstanceContextModel>>(getOdsInstanceContextsQuery.Execute(commonQueryParams));
        return Task.FromResult(Results.Ok(odsInstanceContextList));
    }

    internal static Task<IResult> GetOdsInstanceContext(IGetOdsInstanceContextByIdQuery getOdsInstanceContextByIdQuery, IMapper mapper, int id)
    {
        var odsInstanceContext = getOdsInstanceContextByIdQuery.Execute(id);
        var model = mapper.Map<OdsInstanceContextModel>(odsInstanceContext);
        return Task.FromResult(Results.Ok(model));
    }
}
