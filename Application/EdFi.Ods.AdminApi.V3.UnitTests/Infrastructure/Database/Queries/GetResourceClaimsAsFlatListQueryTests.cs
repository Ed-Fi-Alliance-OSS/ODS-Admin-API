// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetResourceClaimsAsFlatListQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetResourceClaimsFlatV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_ReturnsFlatListOrderedByName()
    {
        using var ctx = CreateContext();
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "zebra", ClaimName = "zebra" });
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "alpha", ClaimName = "alpha" });
        ctx.SaveChanges();
        var result = new GetResourceClaimsAsFlatListQuery(ctx).Execute();
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("alpha");
    }

    [Test]
    public void Execute_WhenEmpty_ReturnsEmpty()
    {
        using var ctx = CreateContext();
        new GetResourceClaimsAsFlatListQuery(ctx).Execute().ShouldBeEmpty();
    }
}
