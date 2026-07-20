// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.Profiles;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Profiles;

[TestFixture]
public class ReadProfileTests
{
    [Test]
    public async Task GetProfiles_ReturnsOkWithMappedProfiles()
    {
        var query = A.Fake<IGetProfilesQuery>();
        var commonQueryParams = new CommonQueryParams();
        A.CallTo(() => query.Execute(commonQueryParams, 5, "Assessment"))
            .Returns(new List<Profile> { new() { ProfileId = 5, ProfileName = "Assessment" } });

        var result = await ReadProfile.GetProfiles(query, commonQueryParams, 5, "Assessment");

        var ok = result.ShouldBeOfType<Ok<List<ProfileModel>>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.Count.ShouldBe(1);
        ok.Value[0].Id.ShouldBe(5);
        ok.Value[0].Name.ShouldBe("Assessment");
    }

    [Test]
    public async Task GetProfile_ReturnsOkWithProfileDetails()
    {
        var query = A.Fake<IGetProfileByIdQuery>();
        A.CallTo(() => query.Execute(5))
            .Returns(new Profile { ProfileId = 5, ProfileName = "Assessment", ProfileDefinition = "<Profile />" });

        var result = await ReadProfile.GetProfile(query, 5);

        var ok = result.ShouldBeOfType<Ok<ProfileDetailsModel>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.Id.ShouldBe(5);
        ok.Value.Name.ShouldBe("Assessment");
        ok.Value.Definition.ShouldBe("<Profile />");
    }
}
