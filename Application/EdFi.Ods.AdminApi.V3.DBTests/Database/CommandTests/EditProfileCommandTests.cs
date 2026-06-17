// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;
using Profile = EdFi.Admin.DataAccess.Models.Profile;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.CommandTests;

[TestFixture]
public class EditProfileCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldEditProfile()
    {
        var profileName = "Test-Profile";
        var definition = "<Profile name=\"Test-Profile\"><Resource name=\"School\"><ReadContentType memberSelection=\"IncludeOnly\">" +
           "<Collection name=\"EducationOrganizationAddresses\" memberSelection=\"IncludeOnly\"><Property name=\"City\" />" +
           "<Property name=\"StateAbbreviationType\" /><Property name=\"PostalCode\" /></Collection>" +
           "</ReadContentType><WriteContentType memberSelection=\"IncludeOnly\">" +
           "<Collection name=\"EducationOrganizationAddresses\" memberSelection=\"IncludeOnly\"><Property name=\"Latitude\" />" +
           "<Property name=\"Longitude\" /></Collection></WriteContentType></Resource></Profile>";
        var newProfile = new Profile()
        {
            ProfileName = profileName,
            ProfileDefinition = definition,
        };

        Save(newProfile);    

        var updateProfileName = "Test-Profile-Update";
        var editProfile = new Mock<IEditProfileModel>();
        editProfile.Setup(x => x.Name).Returns(updateProfileName);
        var updateDefinition = "<Profile name=\"Test-Profile-Update\"><Resource name=\"School\"><ReadContentType memberSelection=\"IncludeOnly\">" +
            "<Collection name=\"EducationOrganizationAddresses\" memberSelection=\"IncludeOnly\"><Property name=\"City\" />" +
            "<Property name=\"StateAbbreviationType\" /><Property name=\"PostalCode\" /></Collection>" +
            "</ReadContentType><WriteContentType memberSelection=\"IncludeOnly\">" +
            "<Collection name=\"EducationOrganizationAddresses\" memberSelection=\"IncludeOnly\"><Property name=\"Latitude\" />" +
            "<Property name=\"Longitude\" /></Collection></WriteContentType></Resource></Profile>";
        editProfile.Setup(x => x.Definition).Returns(updateDefinition);
        editProfile.Setup(x => x.Id).Returns(newProfile.ProfileId);
                
        Transaction(usersContext =>
        {
            var command = new EditProfileCommand(usersContext);
            var updatedProfile = command.Execute(editProfile.Object);
            updatedProfile.ShouldNotBeNull();
            updatedProfile.ProfileId.ShouldBeGreaterThan(0);
            updatedProfile.ProfileId.ShouldBe(newProfile.ProfileId);        
            updatedProfile.ProfileName.ShouldBe(updateProfileName);
            updatedProfile.ProfileDefinition.ShouldBe(updateDefinition);
        });
    }
}


