// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using SecurityAction = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetAllActionsQueryTests
{
    [Test]
    public void Execute_ReturnsAllActions()
    {
        var securityContext = A.Fake<ISecurityContext>();
        SetupActionsDbSet(securityContext,
            new SecurityAction { ActionId = 1, ActionName = "Create", ActionUri = "/create" },
            new SecurityAction { ActionId = 2, ActionName = "Read", ActionUri = "/read" });

        var options = Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
        var query = new GetAllActionsQuery(securityContext, options);

        var result = query.Execute();

        result.Count.ShouldBe(2);
        result.Select(x => x.ActionName).ShouldContain("Create");
        result.Select(x => x.ActionName).ShouldContain("Read");
    }

    [Test]
    public void Execute_WithIdFilter_ReturnsSingleMatch()
    {
        var securityContext = A.Fake<ISecurityContext>();
        SetupActionsDbSet(securityContext,
            new SecurityAction { ActionId = 1, ActionName = "Create", ActionUri = "/create" },
            new SecurityAction { ActionId = 2, ActionName = "Read", ActionUri = "/read" });

        var options = Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
        var query = new GetAllActionsQuery(securityContext, options);

        var result = query.Execute(new CommonQueryParams(0, 25), 2, null);

        result.Count.ShouldBe(1);
        result.Single().ActionName.ShouldBe("Read");
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsSingleMatch()
    {
        var securityContext = A.Fake<ISecurityContext>();
        SetupActionsDbSet(securityContext,
            new SecurityAction { ActionId = 1, ActionName = "Create", ActionUri = "/create" },
            new SecurityAction { ActionId = 2, ActionName = "Read", ActionUri = "/read" });

        var options = Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
        var query = new GetAllActionsQuery(securityContext, options);

        var result = query.Execute(new CommonQueryParams(0, 25), null, "Create");

        result.Count.ShouldBe(1);
        result.Single().ActionId.ShouldBe(1);
    }

    private static void SetupActionsDbSet(ISecurityContext securityContext, params SecurityAction[] actions)
    {
        var data = actions.ToList();
        var queryable = data.AsQueryable();

        var fakeDbSet = A.Fake<DbSet<SecurityAction>>(opts => opts.Implements(typeof(IQueryable<SecurityAction>)));
        A.CallTo(() => ((IQueryable<SecurityAction>)fakeDbSet).Provider).Returns(queryable.Provider);
        A.CallTo(() => ((IQueryable<SecurityAction>)fakeDbSet).Expression).Returns(queryable.Expression);
        A.CallTo(() => ((IQueryable<SecurityAction>)fakeDbSet).ElementType).Returns(queryable.ElementType);
        A.CallTo(() => ((IQueryable<SecurityAction>)fakeDbSet).GetEnumerator()).Returns(queryable.GetEnumerator());

        A.CallTo(() => securityContext.Actions).Returns(fakeDbSet);
    }
}



