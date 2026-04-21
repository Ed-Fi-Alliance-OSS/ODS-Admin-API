// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetAllClaimSetsQueryTests : SecurityDataTestBase
{
    public GetAllClaimSetsQueryTests()
    {
        SeedSecurityContextOnFixtureSetup = true;
    }

    [Test]
    public void Should_Retreive_ClaimSetNames()
    {
        var claimSet1 = GetClaimSet();
        var claimSet2 = GetClaimSet();
        Save(claimSet1, claimSet2);

        var claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
            return query.Execute().Select(x => x.Name).ToArray();
        });

        claimSetNames.ShouldContain(claimSet1.ClaimSetName);
        claimSetNames.ShouldContain(claimSet2.ClaimSetName);
    }

    [Test]
    public void Should_Retreive_ClaimSetNames_With_Offset_And_Limit()
    {
        var claimSet1 = GetClaimSet();
        var claimSet2 = GetClaimSet();
        Save(claimSet1, claimSet2);

        var claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
            return query.Execute(
                new CommonQueryParams(),
                null,
                null).Select(x => x.Name).ToArray();
        });

        claimSetNames.ShouldContain(claimSet1.ClaimSetName);
        claimSetNames.ShouldContain(claimSet2.ClaimSetName);
    }

    [Test]
    public void Should_Retreive_ClaimSetNames_With_Id()
    {
        var claimSet1 = GetClaimSet();
        var claimSet2 = GetClaimSet();
        Save(claimSet1, claimSet2);

        var claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
            return query.Execute(new CommonQueryParams(),
                claimSet2.ClaimSetId,
                null).Select(x => x.Name).ToArray();
        });

        claimSetNames.Length.ShouldBe(1);
        claimSetNames.ShouldContain(claimSet2.ClaimSetName);
    }

    [Test]
    public void Should_Retreive_ClaimSetNames_With_Name()
    {
        var claimSet1 = GetClaimSet();
        var claimSet2 = GetClaimSet();
        Save(claimSet1, claimSet2);

        var claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
            return query.Execute(new CommonQueryParams(),
                null,
                claimSet2.ClaimSetName).Select(x => x.Name).ToArray();
        });

        claimSetNames.Length.ShouldBe(1);
        claimSetNames.ShouldContain(claimSet2.ClaimSetName);
    }

    private static int _claimSetId = 0;
    private static ClaimSet GetClaimSet()
    {
        return new ClaimSet
        {
            ClaimSetName = $"Test Claim Set {_claimSetId++} - {DateTime.Now:O}"
        };
    }
}



