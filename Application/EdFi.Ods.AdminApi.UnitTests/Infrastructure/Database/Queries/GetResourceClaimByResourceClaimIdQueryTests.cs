// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetResourceClaimByResourceClaimIdQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetResourceClaimById_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenResourceClaimExists_ReturnsResourceClaim()
    {
        using var ctx = CreateContext();
        var rc = new ResourceClaim { ResourceName = "students", ClaimName = "students" };
        ctx.ResourceClaims.Add(rc);
        ctx.SaveChanges();

        var result = new GetResourceClaimByResourceClaimIdQuery(ctx).Execute(rc.ResourceClaimId);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("students");
        result.Id.ShouldBe(rc.ResourceClaimId);
    }

    [Test]
    public void Execute_WhenResourceClaimNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetResourceClaimByResourceClaimIdQuery(ctx).Execute(9999));
    }

    [Test]
    public void Execute_WhenParentHasChildren_ReturnsWithChildList()
    {
        using var ctx = CreateContext();
        var parent = new ResourceClaim { ResourceName = "parentRC", ClaimName = "parentRC" };
        ctx.ResourceClaims.Add(parent);
        ctx.SaveChanges();

        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "childRC", ClaimName = "childRC", ParentResourceClaim = parent });
        ctx.SaveChanges();

        var result = new GetResourceClaimByResourceClaimIdQuery(ctx).Execute(parent.ResourceClaimId);

        result.Children.Count.ShouldBe(1);
        result.Children[0].Name.ShouldBe("childRC");
    }
}
