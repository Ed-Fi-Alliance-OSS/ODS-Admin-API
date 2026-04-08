// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V1.Features.OdsInstances;

public class ReadOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances", GetOdsInstances)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceModel[]>(200))
            .BuildForVersions(AdminApiVersions.V1);

        AdminApiEndpointBuilder.MapGet(endpoints, "/odsInstances/{id}", GetOdsInstance)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<OdsInstanceModel>(200))
            .BuildForVersions(AdminApiVersions.V1);
    }

    internal Task<IResult> GetOdsInstances(IGetOdsInstancesQuery getOdsInstancesQuery, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var odsInstances = OdsInstanceMapper.ToModelList(getOdsInstancesQuery.Execute(
            commonQueryParams));
        return Task.FromResult(AdminApiResponse<List<OdsInstanceModel>>.Ok(odsInstances));
    }

    internal Task<IResult> GetOdsInstance(IGetOdsInstanceQuery getOdsInstanceQuery, int id)
    {
        var odsInstance = getOdsInstanceQuery.Execute(id);
        if (odsInstance == null)
        {
            throw new NotFoundException<int>("odsInstance", id);
        }
        var model = OdsInstanceMapper.ToModel(odsInstance);
        return Task.FromResult(AdminApiResponse<OdsInstanceModel>.Ok(model));
    }
}
