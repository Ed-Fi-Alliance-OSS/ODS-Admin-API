// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.CommandTests;

[TestFixture]
public class DeleteProfileCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldDeleteProfile()
    {
        var newProfile = new Profile()
        {
            ProfileName = "Test"
        };
        Save(newProfile);
        var profileId = newProfile.ProfileId;

        Transaction(usersContext =>
        {
            var deleteProfileCommand = new DeleteProfileCommand(usersContext);
            deleteProfileCommand.Execute(profileId);
        });

        Transaction(usersContext => usersContext.Profiles.Where(v => v.ProfileId == profileId).ToArray()).ShouldBeEmpty();
    }   
}


