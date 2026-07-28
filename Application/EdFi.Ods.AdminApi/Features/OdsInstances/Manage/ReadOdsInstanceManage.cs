// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public class ReadOdsInstanceManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances/manage", GetOdsInstanceManages)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceManageModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances/manage/{id}", GetOdsInstanceManage)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceManageModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static Task<IResult> GetOdsInstanceManages(IGetOdsInstanceManagesQuery query,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var list = OdsInstanceManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetOdsInstanceManage(IGetOdsInstanceManageByIdQuery query, int id)
    {
        var odsInstanceManage = query.Execute(id);
        if (odsInstanceManage == null)
        {
            throw new NotFoundException<int>("odsInstanceManage", id);
        }
        var model = OdsInstanceManageMapper.ToModel(odsInstanceManage);
        return Task.FromResult(Results.Ok(model));
    }
}
