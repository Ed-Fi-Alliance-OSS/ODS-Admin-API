// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditProfileCommand
{
    Profile Execute(IEditProfileModel changedProfileData);
}

public class EditProfileCommand : IEditProfileCommand
{
    private readonly IUsersContext _context;

    public EditProfileCommand(IUsersContext context)
    {
        _context = context;
    }

    public Profile Execute(IEditProfileModel changedProfileData)
    {
        var profile = _context.Profiles.SingleOrDefault(v => v.ProfileId == changedProfileData.Id) ??
            throw new NotFoundException<int>("profile", changedProfileData.Id);

        profile.ProfileName = changedProfileData.Name;
        profile.ProfileDefinition = changedProfileData.Definition;

        _context.SaveChanges();
        return profile;
    }
}

public interface IEditProfileModel
{
    public int Id { get; set; }
    string? Name { get; }
    string? Definition { get; }
}
