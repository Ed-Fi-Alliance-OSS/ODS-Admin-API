// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetAllActionsQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetAllActions()
    {
        LoadSeedData();
        using var securityContext = TestContext;
        var query = new GetAllActionsQuery(securityContext, Testing.GetAppSettings());
        var resultNames = query.Execute().Select(x => x.ActionName).ToList();

        resultNames.Count.ShouldBe(4);

        resultNames.ShouldContain("Create");
        resultNames.ShouldContain("Read");
    }

    [Test]
    public void ShouldGetAllActions_With_Offset_and_Limit_and_order_by_id_Asc()
    {
        LoadSeedData();
        var offset = 1;
        var limit = 2;

        using var securityContext = TestContext;
        var query = new GetAllActionsQuery(securityContext, Testing.GetAppSettings());
        var resultNames = query.Execute(
            new CommonQueryParams(offset, limit, "id", "Ascending"),
            null, null).Select(x => x.ActionName).ToList();

        resultNames.Count.ShouldBe(2);

        resultNames.ShouldContain("Create");
        resultNames.ShouldContain("Update");
    }

    [Test]
    public void ShouldGetAllActions_With_Offset_and_Limit_and_order_by_Name_Asc()
    {
        LoadSeedData();
        var offset = 1;
        var limit = 2;

        using var securityContext = TestContext;
        var query = new GetAllActionsQuery(securityContext, Testing.GetAppSettings());
        var resultNames = query.Execute(
            new CommonQueryParams(offset, limit, "Name", "Ascending"),
            null, null).Select(x => x.ActionName).ToList();

        resultNames.Count.ShouldBe(2);

        resultNames.ShouldContain("Delete");
        resultNames.ShouldContain("Read");
    }

    [Test]
    public void ShouldGetAllActions_With_Offset_and_Limit_and_order_by_Name_Desc()
    {
        LoadSeedData();
        var offset = 1;
        var limit = 2;

        using var securityContext = TestContext;
        var query = new GetAllActionsQuery(securityContext, Testing.GetAppSettings());
        var resultNames = query.Execute(
            new CommonQueryParams(offset, limit, "Name", "Descending"),
            null, null).Select(x => x.ActionName).ToList();

        resultNames.Count.ShouldBe(2);

        resultNames.ShouldContain("Read");
        resultNames.ShouldContain("Delete");
    }

    [Test]
    public void ShouldGetAllActions_With_Name()
    {
        LoadSeedData();
        var name = "Delete";
        using var securityContext = TestContext;
        var query = new GetAllActionsQuery(securityContext, Testing.GetAppSettings());
        var resultNames = query.Execute(
            new CommonQueryParams(),
            null, name).Select(x => x.ActionName).ToList();

        resultNames.Count.ShouldBe(1);

        resultNames.ShouldContain("Delete");
    }
}
