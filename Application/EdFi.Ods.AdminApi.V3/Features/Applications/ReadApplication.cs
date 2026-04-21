// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.V3.Features.Applications;

public class ReadApplication : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/applications", GetApplications)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/applications/{id}", GetApplication)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApplicationModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static async Task<IResult> GetApplications(
        IGetAllApplicationsQuery getAllApplicationsQuery,
        IGetOdsInstanceIdsByApplicationIdQuery getOdsInstanceIdsByApplicationIdQuery,
        Validator validator,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? applicationName, string? claimsetName, string? ids)
    {
        if (!string.IsNullOrEmpty(ids))
        {
            await validator.GuardAsync(ids);
        }
        var applicationEntities = getAllApplicationsQuery.Execute(commonQueryParams, id, applicationName, claimsetName, ids);
        var odsInstanceIdsByApplicationId = getOdsInstanceIdsByApplicationIdQuery.Execute(applicationEntities.Select(a => a.ApplicationId));
        var applications = ApplicationMapper.ToModelList(applicationEntities, odsInstanceIdsByApplicationId);
        return Results.Ok(applications);
    }

    internal static Task<IResult> GetApplication(GetApplicationByIdQuery getApplicationByIdQuery, IGetOdsInstanceIdsByApplicationIdQuery getOdsInstanceIdsByApplicationIdQuery, int id)
    {
        var application = getApplicationByIdQuery.Execute(id);
        if (application == null)
        {
            throw new NotFoundException<int>("application", id);
        }
        var odsInstanceIds = getOdsInstanceIdsByApplicationIdQuery.Execute(application.ApplicationId);
        var model = ApplicationMapper.ToModel(application, odsInstanceIds);
        return Task.FromResult(Results.Ok(model));
    }

    public class Validator : AbstractValidator<string>
    {
        public Validator()
        {
            RuleFor(ids => ids)
            .Must(ids => Array.TrueForAll(ids.Split(','), id => int.TryParse(id.Trim(), out _)))
                .WithMessage("The 'ids' query parameter must be a comma-separated list of integers.");
        }
    }
}



