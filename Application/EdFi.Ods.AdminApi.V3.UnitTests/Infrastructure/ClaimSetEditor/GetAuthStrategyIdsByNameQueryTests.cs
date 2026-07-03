// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetAuthStrategyIdsByNameQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetAuthStratIdsByNameV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenNamesMatch_ReturnsIds()
    {
        using var ctx = CreateContext();
        ctx.AuthorizationStrategies.Add(new SecurityAuthorizationStrategy { AuthorizationStrategyName = "StratA", DisplayName = "StratA" });
        ctx.SaveChanges();
        var getAllQuery = new GetAllAuthorizationStrategiesQuery(ctx);
        var result = new GetAuthStrategyIdsByNameQuery(getAllQuery).Execute(new[] { "StratA" });
        result.Count.ShouldBe(1);
        result[0].ShouldBeGreaterThan(0);
    }

    [Test]
    public void Execute_WhenNameNotFound_ThrowsAdminApiException()
    {
        using var ctx = CreateContext();
        ctx.SaveChanges();
        var getAllQuery = new GetAllAuthorizationStrategiesQuery(ctx);
        Should.Throw<EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling.AdminApiException>(
            () => new GetAuthStrategyIdsByNameQuery(getAllQuery).Execute(new[] { "Unknown" }));
    }
}
