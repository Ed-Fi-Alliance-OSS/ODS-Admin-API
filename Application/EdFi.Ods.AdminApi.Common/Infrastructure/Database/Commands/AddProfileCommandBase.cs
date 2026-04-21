// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class AddProfileCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected Profile ExecuteCore(string? name, string? definition)
    {
        var profile = new Profile
        {
            ProfileName = name,
            ProfileDefinition = definition
        };
        _context.Profiles.Add(profile);
        _context.SaveChanges();
        return profile;
    }
}
