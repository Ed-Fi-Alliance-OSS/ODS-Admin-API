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
public class GetChildResourceClaimsForParentQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetChildRCV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenParentHasChildren_ReturnsChildren()
    {
        using var ctx = CreateContext();
        var parent = new ResourceClaim { ResourceName = "parent", ClaimName = "parent" };
        ctx.ResourceClaims.Add(parent);
        ctx.SaveChanges();
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "child1", ClaimName = "child1", ParentResourceClaim = parent });
        ctx.SaveChanges();
        var result = new GetChildResourceClaimsForParentQuery(ctx).Execute(parent.ResourceClaimId).ToList();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("child1");
    }

    [Test]
    public void Execute_WhenNoChildren_ReturnsEmpty()
    {
        using var ctx = CreateContext();
        var parent = new ResourceClaim { ResourceName = "lonelyParent", ClaimName = "lonelyParent" };
        ctx.ResourceClaims.Add(parent);
        ctx.SaveChanges();
        new GetChildResourceClaimsForParentQuery(ctx).Execute(parent.ResourceClaimId).ShouldBeEmpty();
    }
}
