// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.V2.Features.ApiClients;

public class ReadApiClient : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/apiclients", GetApiClients)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApiClientModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/apiclients/{id}", GetApiClient)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApiClientModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static Task<IResult> GetApiClients(
        [FromServices] IGetApiClientsByApplicationIdQuery getApiClientsByApplicationIdQuery,
        [FromServices] IGetOdsInstanceIdsByApiClientIdQuery getOdsInstanceIdsByApiClientIdQuery,
        [FromQuery(Name = "applicationid")] int applicationid)
    {
        var apiClients = getApiClientsByApplicationIdQuery.Execute(applicationid);
        var odsInstanceIdsByApiClientId = getOdsInstanceIdsByApiClientIdQuery.Execute(apiClients.Select(a => a.ApiClientId));
        var models = ApiClientMapper.ToModelList(apiClients, odsInstanceIdsByApiClientId);
        return Task.FromResult(Results.Ok(models));
    }

    public static Task<IResult> GetApiClient(
        [FromServices] IGetApiClientByIdQuery getApiClientByIdQuery,
        [FromServices] IGetOdsInstanceIdsByApiClientIdQuery getOdsInstanceIdsByApiClientIdQuery,
        int id)
    {
        var apiClient = getApiClientByIdQuery.Execute(id) ?? throw new NotFoundException<int>("apiClient", id);
        var odsInstanceIds = getOdsInstanceIdsByApiClientIdQuery.Execute(apiClient.ApiClientId);
        var model = ApiClientMapper.ToModel(apiClient, odsInstanceIds);
        return Task.FromResult(Results.Ok(model));
    }
}
