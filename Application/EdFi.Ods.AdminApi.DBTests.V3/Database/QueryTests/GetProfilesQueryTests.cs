// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.QueryTests;

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
            var query = new GetProfilesQuery(usersContext, Testing.GetAppSettings());
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

    [Test]
    public void Should_retreive_profiles_with_offset_limit()
    {
        var profile1 = CreateProfile();
        var profile2 = CreateProfile();
        var profile3 = CreateProfile();

        Save(profile1, profile2, profile3);

        List<Profile> results = null;
        Transaction(usersContext =>
        {
            var query = new GetProfilesQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute(new CommonQueryParams(1, 1), null, null);
            results.Count.ShouldBe(1);
        });
        results.Any(p => p.ProfileName == profile2.ProfileName).ShouldBeTrue();
    }

    [Test]
    public void Should_retreive_profiles_with_id()
    {
        var profile1 = CreateProfile();
        var profile2 = CreateProfile();

        Save(profile1, profile2);

        List<Profile> results = null;
        Transaction(usersContext =>
        {
            var query = new GetProfilesQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute(new CommonQueryParams(0, 5), profile2.ProfileId, null);
            results.Count.ShouldBe(1);
        });
        results.Any(p => p.ProfileName == profile2.ProfileName).ShouldBeTrue();
    }

    [Test]
    public void Should_retreive_profiles_with_name()
    {
        var profile1 = CreateProfile();
        var profile2 = CreateProfile();

        Save(profile1, profile2);

        List<Profile> results = null;
        Transaction(usersContext =>
        {
            var query = new GetProfilesQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute(new CommonQueryParams(0, 5), null, profile2.ProfileName);
            results.Count.ShouldBe(1);
        });
        results.Any(p => p.ProfileName == profile2.ProfileName).ShouldBeTrue();
    }
}
