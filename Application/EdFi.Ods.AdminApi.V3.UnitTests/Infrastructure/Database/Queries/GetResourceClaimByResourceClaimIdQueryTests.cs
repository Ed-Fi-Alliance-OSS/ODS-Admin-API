// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetResourceClaimByResourceClaimIdQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetRCByIdV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenExists_ReturnsResourceClaim()
    {
        using var ctx = CreateContext();
        var rc = new ResourceClaim { ResourceName = "schools", ClaimName = "schools" };
        ctx.ResourceClaims.Add(rc);
        ctx.SaveChanges();
        var result = new GetResourceClaimByResourceClaimIdQuery(ctx).Execute(rc.ResourceClaimId);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("schools");
        result.Id.ShouldBe(rc.ResourceClaimId);
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetResourceClaimByResourceClaimIdQuery(ctx).Execute(9999));
    }
}
