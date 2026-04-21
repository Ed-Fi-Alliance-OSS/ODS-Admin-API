// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditProfileCommand
{
    Profile Execute(IEditProfileModel changedProfileData);
}

public class EditProfileCommand(IUsersContext context) : EditProfileCommandBase(context), IEditProfileCommand
{
    public Profile Execute(IEditProfileModel changedProfileData)
    {
        return ExecuteCore(changedProfileData.Id, changedProfileData.Name, changedProfileData.Definition);
    }
}

public interface IEditProfileModel
{
    public int Id { get; set; }
    string? Name { get; }
    string? Definition { get; }
}
