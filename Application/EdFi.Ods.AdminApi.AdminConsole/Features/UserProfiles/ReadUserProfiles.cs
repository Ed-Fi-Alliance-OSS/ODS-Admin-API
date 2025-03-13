// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.UserProfiles.Queries;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.UserProfiles;

public class ReadUserProfiles : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/userprofile", GetUserProfiles)
            .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/userprofile/{tenantId}/{id}", GetUserProfileById)
            .WithRouteOptions(b => b.WithResponse<UserProfileModel>(200))
            .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/userprofile/{tenantId}", GetUserProfilesByTenant)
            .WithRouteOptions(b => b.WithResponse<UserProfileModel>(200))
            .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    internal static async Task<IResult> GetUserProfileById(IMapper mapper, IGetUserProfileByIdQuery getUserProfileQuery, int tenantId, int id)
    {
        var userProfile = await getUserProfileQuery.Execute(tenantId, id);
        if (userProfile != null)
            return Results.Ok(userProfile);
        return Results.NotFound();
    }

    internal static async Task<IResult> GetUserProfiles(IMapper mapper, IGetUserProfilesQuery getUserProfilesQuery)
    {
        var userProfiles = await getUserProfilesQuery.Execute();
        return Results.Ok(userProfiles);
    }

    internal static async Task<IResult> GetUserProfilesByTenant(IMapper mapper, IGetUserProfilesByTenantIdQuery getUserProfilesQuery, int tenantId)
    {
        var userProfiles = await getUserProfilesQuery.Execute(tenantId);
        if (userProfiles.Any())
        {
            return Results.Ok(userProfiles);
        }
        return Results.NotFound();
    }
}
