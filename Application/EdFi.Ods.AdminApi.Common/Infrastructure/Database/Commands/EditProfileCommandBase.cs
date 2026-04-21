// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class EditProfileCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected Profile ExecuteCore(int id, string? name, string? definition)
    {
        var profile = _context.Profiles.SingleOrDefault(v => v.ProfileId == id)
            ?? throw new NotFoundException<int>("profile", id);

        profile.ProfileName = name;
        profile.ProfileDefinition = definition;

        _context.SaveChanges();
        return profile;
    }
}
