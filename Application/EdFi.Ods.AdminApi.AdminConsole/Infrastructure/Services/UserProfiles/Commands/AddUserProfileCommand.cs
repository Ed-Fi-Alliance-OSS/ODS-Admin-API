// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.UserProfiles.Commands;

public interface IAddUserProfileCommand
{
    Task<UserProfile> Execute(IAddUserProfileModel newUserProfile);
}

public class AddUserProfileCommand(ICommandRepository<UserProfile> userProfileCommand) : IAddUserProfileCommand
{
    private readonly ICommandRepository<UserProfile> _userProfileCommand = userProfileCommand;

    public async Task<UserProfile> Execute(IAddUserProfileModel newUserProfile)
    {
        return await _userProfileCommand.AddAsync(new UserProfile
        {
            InstanceId = newUserProfile.InstanceId,
            TenantId = newUserProfile.TenantId,
            EdOrgId = newUserProfile.EdOrgId,
            Document = newUserProfile.Document,
        });
    }
}

public interface IAddUserProfileModel
{
    int InstanceId { get; }
    int? EdOrgId { get; }
    int TenantId { get; }
    string Document { get; }
}
