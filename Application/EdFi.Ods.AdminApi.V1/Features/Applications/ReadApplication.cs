// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V1.Features.Applications;

public class ReadApplication : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/applications", GetApplications)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V1);

        AdminApiEndpointBuilder.MapGet(endpoints, "/applications/{id}", GetApplication)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApplicationModel>(200))
            .BuildForVersions(AdminApiVersions.V1);
    }

    internal Task<IResult> GetApplications(
        IGetApplicationsQuery getApplicationsAndApplicationsQuery, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var applications = getApplicationsAndApplicationsQuery.Execute(commonQueryParams);
        return Task.FromResult(AdminApiResponse<List<ApplicationModel>>.Ok(mapper.Map<List<ApplicationModel>>(applications)));
    }

    internal Task<IResult> GetApplication(GetApplicationByIdQuery getApplicationByIdQuery, IMapper mapper, int id)
    {
        var application = getApplicationByIdQuery.Execute(id);
        if (application == null)
        {
            throw new NotFoundException<int>("application", id);
        }
        var model = mapper.Map<ApplicationModel>(application);
        return Task.FromResult(AdminApiResponse<ApplicationModel>.Ok(model));
    }
}
