// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.QueryTests;

[TestFixture]
public class GetProfilesQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_retreive_profiles()
    {
        var profile1 = CreateProfile();
        var profile2 = CreateProfile();

        Save(profile1, profile2);

        List<Profile> results = null;
        Transaction(usersContext =>
        {
            var query = new GetProfilesQuery(usersContext);
            results = query.Execute();
        });

        results.Any(p => p.ProfileName == profile1.ProfileName).ShouldBeTrue();
        results.Any(p => p.ProfileName == profile2.ProfileName).ShouldBeTrue();
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
