// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.AdminConsole.Helpers;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.UserProfiles.Queries;

public interface IGetUserProfileByIdQuery
{
    Task<UserProfile?> Execute(int tenantId, int docId);
}

public class GetUserProfileByIdQuery : IGetUserProfileByIdQuery
{
    private readonly IQueriesRepository<UserProfile> _userProfileQuery;

    public GetUserProfileByIdQuery(IQueriesRepository<UserProfile> userProfileQuery)
    {
        _userProfileQuery = userProfileQuery;
    }

    public async Task<UserProfile?> Execute(int tenantId, int docId)
    {
        var userProfile = await _userProfileQuery.Query().SingleOrDefaultAsync(UserProfile => UserProfile.DocId == docId && UserProfile.TenantId == tenantId);
        return userProfile;
    }
}
