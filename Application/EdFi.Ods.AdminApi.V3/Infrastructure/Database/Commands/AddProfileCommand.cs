// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IAddProfileCommand
{
    Profile Execute(IAddProfileModel newProfile);
}

public class AddProfileCommand : IAddProfileCommand
{
    private readonly IUsersContext _context;

    public AddProfileCommand(IUsersContext context)
    {
        _context = context;
    }

    public Profile Execute(IAddProfileModel newProfile)
    {      
        var profile = new Profile
        {
           ProfileName = newProfile.Name,
           ProfileDefinition = newProfile.Definition
        };
        _context.Profiles.Add(profile);
        _context.SaveChanges();
        return profile;
    }
}

public interface IAddProfileModel
{
    string? Name { get; }
    string? Definition { get; }
}
