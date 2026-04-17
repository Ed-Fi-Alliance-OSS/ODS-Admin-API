// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;

public interface IGetProfileByIdQuery
{
    Profile Execute(int profileId);
}

public class GetProfileByIdQuery : IGetProfileByIdQuery
{
    private readonly IUsersContext _context;

    public GetProfileByIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public Profile Execute(int profileId)
    {
        var profile = _context.Profiles.SingleOrDefault(app => app.ProfileId == profileId);
        if (profile == null)
        {
            throw new NotFoundException<int>("profile", profileId);
        }

        return profile;
    }
}
