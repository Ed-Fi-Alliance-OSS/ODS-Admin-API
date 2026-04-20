// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetProfileByIdQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_retreive_profile()
    {
        var profile1 = CreateProfile();
        var profile2 = CreateProfile();

        Save(profile1, profile2);

        Profile result = null;
        Transaction(usersContext =>
        {
            var query = new GetProfileByIdQuery(usersContext);
            result = query.Execute(profile2.ProfileId);
            result.ShouldNotBeNull();
            result.ProfileId.ShouldBe(profile2.ProfileId);
            result.ProfileName.ShouldBe(profile2.ProfileName);
        });        
    }

    private static int _profileId = 0;
    private static Profile CreateProfile()
    {
        return new Profile
        {
            ProfileName = $"Test Profile {_profileId++}-{DateTime.Now:O}"
        };
    }
}


