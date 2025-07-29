// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Features.Applications;

public class ReadApplication : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/applications", GetApplications)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/applications/{id}", GetApplication)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ApplicationModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static async Task<IResult> GetApplications(
        IGetAllApplicationsQuery getAllApplicationsQuery,
        IMapper mapper,
        IOptions<AppSettings> settings,
        Validator validator,
        [AsParameters] CommonQueryParams commonQueryParams, int? id, string? applicationName, string? claimsetName, string? ids)
    {
        if (!string.IsNullOrEmpty(ids))
        {
            await validator.GuardAsync(ids);
        }
        var applications = mapper.Map<List<ApplicationModel>>(getAllApplicationsQuery.Execute(commonQueryParams, id, applicationName, claimsetName, ids));
        return Results.Ok(applications);
    }

    internal static Task<IResult> GetApplication(GetApplicationByIdQuery getApplicationByIdQuery, IMapper mapper, int id)
    {
        var application = getApplicationByIdQuery.Execute(id);
        if (application == null)
        {
            throw new NotFoundException<int>("application", id);
        }
        var model = mapper.Map<ApplicationModel>(application);
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
