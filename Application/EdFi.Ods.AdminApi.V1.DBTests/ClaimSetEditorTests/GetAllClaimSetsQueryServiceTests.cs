// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;
using ClaimSetModel = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V1.DBTests.ClaimSetEditorTests;

[TestFixture]
public class GetAllClaimSetsQueryServiceTests : SecurityDataTestBase
{
    public GetAllClaimSetsQueryServiceTests()
    {
        SeedSecurityContextOnFixtureSetup = true;
    }

    [Test]
    public void Should_Retrieve_ClaimSetNames()
    {
        var application = new Security.DataAccess.Models.Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(application);

        var claimSet1 = GetClaimSet(application);
        var claimSet2 = GetClaimSet(application);
        Save(claimSet1, claimSet2);

        var claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQueryService(securityContext, Testing.GetAppSettings());
            return query.Execute().Select(x => x.Name).ToArray();
        });

        claimSetNames.ShouldContain(claimSet1.ClaimSetName);
        claimSetNames.ShouldContain(claimSet2.ClaimSetName);
    }

    [Test]
    public void Should_Retrieve_EdfiPreset_ClaimSet()
    {
        var application = new Security.DataAccess.Models.Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(application);

        var claimSet1 = GetClaimSet(application, true);
        var claimSet2 = GetClaimSet(application);
        Save(claimSet1, claimSet2);

        var claimSets = Transaction(securityContext =>
        {
            var query = new GetAllClaimSetsQueryService(securityContext, Testing.GetAppSettings());
            return query.Execute().ToArray();
        });

        var edfiPresetClaimSet = claimSets.Where(x => x.Name.Equals(claimSet1.ClaimSetName) && x.IsEditable == false).ToList();
        edfiPresetClaimSet.Count.ShouldBe(1);
    }

    [Test]
    public void Should_Retrieve_ClaimSetNames_with_offset_and_limit()
    {
        var application = new Security.DataAccess.Models.Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(application);

        var claimSet1 = GetClaimSet(application);
        var claimSet2 = GetClaimSet(application);
        var claimSet3 = GetClaimSet(application);
        var claimSet4 = GetClaimSet(application);
        var claimSet5 = GetClaimSet(application);

        Save(claimSet1, claimSet2, claimSet3, claimSet4, claimSet5);

        var commonQueryParams = new CommonQueryParams(0, 2);
        var claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQueryService(securityContext, Testing.GetAppSettings());
            return query.Execute(commonQueryParams).Select(x => x.Name).ToArray();
        });

        claimSetNames.Length.ShouldBe(2);
        claimSetNames.ShouldContain(claimSet1.ClaimSetName);
        claimSetNames.ShouldContain(claimSet2.ClaimSetName);

        commonQueryParams.Offset = 2;
        claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQueryService(securityContext, Testing.GetAppSettings());
            return query.Execute(commonQueryParams).Select(x => x.Name).ToArray();
        });

        claimSetNames.Length.ShouldBe(2);
        claimSetNames.ShouldContain(claimSet3.ClaimSetName);
        claimSetNames.ShouldContain(claimSet4.ClaimSetName);

        commonQueryParams.Offset = 4;
        claimSetNames = Transaction<string[]>(securityContext =>
        {
            var query = new GetAllClaimSetsQueryService(securityContext, Testing.GetAppSettings());
            return query.Execute(commonQueryParams).Select(x => x.Name).ToArray();
        });

        claimSetNames.Length.ShouldBe(1);
        claimSetNames.ShouldContain(claimSet5.ClaimSetName);
    }

    private static int _claimSetId = 0;
    private static ClaimSetModel GetClaimSet(Security.DataAccess.Models.Application application, bool IsEdfiPreset = false)
    {
        return new ClaimSetModel
        {
            Application = application,
            ClaimSetName = $"Test Claim Set {_claimSetId++} - {DateTime.Now:O}",
            IsEdfiPreset = IsEdfiPreset
        };
    }
}
