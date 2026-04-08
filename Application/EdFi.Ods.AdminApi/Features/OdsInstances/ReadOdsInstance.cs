// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public class ReadOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances", GetOdsInstances)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances/{id}", GetOdsInstance)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceDetailModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetOdsInstances(IGetOdsInstancesQuery getOdsInstancesQuery, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name, string? instanceType)
    {
        var odsInstances = OdsInstanceMapper.ToModelList(getOdsInstancesQuery.Execute(
            commonQueryParams,
            id,
            name,
            instanceType));
        return Task.FromResult(Results.Ok(odsInstances));
    }

    internal static Task<IResult> GetOdsInstance(IGetOdsInstanceQuery getOdsInstanceQuery, int id)
    {
        var odsInstance = getOdsInstanceQuery.Execute(id);
        var model = OdsInstanceMapper.ToDetailModel(odsInstance);
        return Task.FromResult(Results.Ok(model));
    }
}
