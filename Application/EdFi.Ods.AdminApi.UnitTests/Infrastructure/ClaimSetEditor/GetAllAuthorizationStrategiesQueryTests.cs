// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetAllAuthorizationStrategiesQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetAllAuthStrategies_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_ReturnsStrategiesOrderedByName()
    {
        using var ctx = CreateContext();
        ctx.AuthorizationStrategies.Add(new SecurityAuthorizationStrategy { AuthorizationStrategyName = "Zebra", DisplayName = "Zebra" });
        ctx.AuthorizationStrategies.Add(new SecurityAuthorizationStrategy { AuthorizationStrategyName = "Alpha", DisplayName = "Alpha" });
        ctx.SaveChanges();

        var result = new GetAllAuthorizationStrategiesQuery(ctx).Execute();

        result.Count.ShouldBe(2);
        result[0].AuthStrategyName.ShouldBe("Alpha");
        result[1].AuthStrategyName.ShouldBe("Zebra");
    }

    [Test]
    public void Execute_WhenEmpty_ReturnsEmptyList()
    {
        using var ctx = CreateContext();
        var result = new GetAllAuthorizationStrategiesQuery(ctx).Execute();
        result.ShouldBeEmpty();
    }
}
