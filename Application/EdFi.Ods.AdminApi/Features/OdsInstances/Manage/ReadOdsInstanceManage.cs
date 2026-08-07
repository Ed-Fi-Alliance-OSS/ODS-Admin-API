// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var list = OdsInstanceManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetOdsInstanceManage(IGetOdsInstanceManageByIdQuery query, int id,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure(nameof(OdsInstance), "This endpoint has been disabled on application settings.")]);

        var odsInstanceManage = query.Execute(id);
        if (odsInstanceManage == null)
        {
            throw new NotFoundException<int>("odsInstanceManage", id);
        }
        var model = OdsInstanceManageMapper.ToModel(odsInstanceManage);
        return Task.FromResult(Results.Ok(model));
    }
}
