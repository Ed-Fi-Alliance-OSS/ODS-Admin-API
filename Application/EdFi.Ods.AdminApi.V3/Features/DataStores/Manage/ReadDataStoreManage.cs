// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public class ReadDataStoreManage : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores/manage", GetDataStoreManages)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreManageModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores/manage/{id}", GetDataStoreManage)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<DataStoreManageModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static Task<IResult> GetDataStoreManages(IGetDataStoreManagesQuery query,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure("dataStore", "This endpoint has been disabled on application settings.")]);

        var list = DataStoreManageMapper.ToModelList(query.Execute(commonQueryParams, id, name));
        return Task.FromResult(Results.Ok(list));
    }

    public static Task<IResult> GetDataStoreManage(IGetDataStoreManageByIdQuery query, int id,
        [FromServices] IOptions<AppSettings> options)
    {
        if (!options.Value.EnableDataStoreManagement)
            throw new ValidationException([new ValidationFailure("dataStore", "This endpoint has been disabled on application settings.")]);

        var dataStoreManage = query.Execute(id);
        if (dataStoreManage == null)
        {
            throw new NotFoundException<int>("dataStoreManage", id);
        }
        var model = DataStoreManageMapper.ToModel(dataStoreManage);
        return Task.FromResult(Results.Ok(model));
    }
}
