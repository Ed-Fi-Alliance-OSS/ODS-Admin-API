// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.


using System.Linq;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.CommandTests;

[TestFixture]
public class AddProfileCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldAddProfile()
    {
        var profileName = "Test-Profile";
        var newProfile = new Mock<IAddProfileModel>();
        newProfile.Setup(x => x.Name).Returns(profileName);
        var definition = "<Profile name=\"Test-Profile\"><Resource name=\"School\"><ReadContentType memberSelection=\"IncludeOnly\">" +
            "<Collection name=\"EducationOrganizationAddresses\" memberSelection=\"IncludeOnly\"><Property name=\"City\" />" +
            "<Property name=\"StateAbbreviationType\" /><Property name=\"PostalCode\" /></Collection>" +
            "</ReadContentType><WriteContentType memberSelection=\"IncludeOnly\">" +
            "<Collection name=\"EducationOrganizationAddresses\" memberSelection=\"IncludeOnly\"><Property name=\"Latitude\" />" +
            "<Property name=\"Longitude\" /></Collection></WriteContentType></Resource></Profile>";
        newProfile.Setup(x => x.Definition).Returns(definition);
  
        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddProfileCommand(usersContext);
            id = command.Execute(newProfile.Object).ProfileId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var profile = usersContext.Profiles.Single(v => v.ProfileId == id);
            profile.ProfileName.ShouldBe(profileName);
            profile.ProfileDefinition.ShouldBe(definition);
        });
    }
}


