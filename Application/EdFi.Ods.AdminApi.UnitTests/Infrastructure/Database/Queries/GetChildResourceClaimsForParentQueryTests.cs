// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetChildResourceClaimsForParentQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetChildResourceClaims_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenParentHasChildren_ReturnsChildrenOrderedByName()
    {
        using var ctx = CreateContext();
        var parent = new ResourceClaim { ResourceName = "parent", ClaimName = "parent" };
        ctx.ResourceClaims.Add(parent);
        ctx.SaveChanges();

        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "childB", ClaimName = "childB", ParentResourceClaim = parent });
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "childA", ClaimName = "childA", ParentResourceClaim = parent });
        ctx.SaveChanges();

        var result = new GetChildResourceClaimsForParentQuery(ctx).Execute(parent.ResourceClaimId).ToList();

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("childA");
        result[1].Name.ShouldBe("childB");
        result.ShouldAllBe(r => r.ParentId == parent.ResourceClaimId);
    }

    [Test]
    public void Execute_WhenParentHasNoChildren_ReturnsEmpty()
    {
        using var ctx = CreateContext();
        var parent = new ResourceClaim { ResourceName = "loneParent", ClaimName = "loneParent" };
        ctx.ResourceClaims.Add(parent);
        ctx.SaveChanges();

        var result = new GetChildResourceClaimsForParentQuery(ctx).Execute(parent.ResourceClaimId);

        result.ShouldBeEmpty();
    }
}
