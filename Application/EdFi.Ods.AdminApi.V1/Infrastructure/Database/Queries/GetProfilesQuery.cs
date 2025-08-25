// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public class GetProfilesQuery
{
    private readonly IUsersContext _usersContext;

    public GetProfilesQuery(IUsersContext usersContext)
    {
        _usersContext = usersContext;
    }

    public List<Profile> Execute()
    {
        return _usersContext.Profiles.OrderBy(p => p.ProfileName).ToList();
    }
}
