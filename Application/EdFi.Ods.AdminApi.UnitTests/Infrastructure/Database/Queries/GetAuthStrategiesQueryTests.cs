// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetAuthStrategiesQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetAuthStrategies_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsAllStrategiesOrderedByName()
    {
        using var ctx = CreateContext();
        ctx.AuthorizationStrategies.Add(new AuthorizationStrategy { AuthorizationStrategyName = "Zebra", DisplayName = "Zebra" });
        ctx.AuthorizationStrategies.Add(new AuthorizationStrategy { AuthorizationStrategyName = "Alpha", DisplayName = "Alpha" });
        ctx.SaveChanges();

        var result = new GetAuthStrategiesQuery(ctx, DefaultOptions()).Execute();

        result.Count.ShouldBe(2);
        result[0].AuthorizationStrategyName.ShouldBe("Alpha");
    }

    [Test]
    public void Execute_WithPagination_ReturnsPaginatedResults()
    {
        using var ctx = CreateContext();
        ctx.AuthorizationStrategies.Add(new AuthorizationStrategy { AuthorizationStrategyName = "A", DisplayName = "A" });
        ctx.AuthorizationStrategies.Add(new AuthorizationStrategy { AuthorizationStrategyName = "B", DisplayName = "B" });
        ctx.AuthorizationStrategies.Add(new AuthorizationStrategy { AuthorizationStrategyName = "C", DisplayName = "C" });
        ctx.SaveChanges();

        var result = new GetAuthStrategiesQuery(ctx, DefaultOptions())
            .Execute(new CommonQueryParams(0, 2));

        result.Count.ShouldBe(2);
    }

    [Test]
    public void Execute_WhenEmpty_ReturnsEmptyList()
    {
        using var ctx = CreateContext();
        var result = new GetAuthStrategiesQuery(ctx, DefaultOptions()).Execute();
        result.ShouldBeEmpty();
    }
}
