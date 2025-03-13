// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.UserProfiles.Queries;

public interface IGetUserProfilesByTenantIdQuery
{
    Task<IEnumerable<UserProfile>> Execute(int tenantId);
}

public class GetUserProfilesByTenantIdQuery(IQueriesRepository<UserProfile> userProfileQuery) : IGetUserProfilesByTenantIdQuery
{
    private readonly IQueriesRepository<UserProfile> _userProfileQuery = userProfileQuery;

    public async Task<IEnumerable<UserProfile>> Execute(int tenantId)
    {
        var userProfiles = await _userProfileQuery.Query().Where(UserProfile => UserProfile.TenantId == tenantId).ToListAsync();
        return userProfiles;
    }
}
