// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.V3.Features.Profiles;

public class ReadProfile : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/profiles", GetProfiles)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ProfileModel[]>(200))
            .BuildForVersions(AdminApiVersions.V3);

        AdminApiEndpointBuilder.MapGet(endpoints, "/profiles/{id}", GetProfile)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<ProfileDetailsModel>(200))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static Task<IResult> GetProfiles(IGetProfilesQuery getProfilesQuery, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name)
    {
        var profileList = ProfileMapper.ToModelList(getProfilesQuery.Execute(
            commonQueryParams,
            id, name));
        return Task.FromResult(Results.Ok(profileList));
    }

    internal static Task<IResult> GetProfile(IGetProfileByIdQuery getProfileByIdQuery, int id)
    {
        var profile = getProfileByIdQuery.Execute(id);
        if (profile == null)
        {
            throw new NotFoundException<int>("profile", id);
        }
        var model = ProfileMapper.ToDetailsModel(profile);
        return Task.FromResult(Results.Ok(model));
    }
}



