// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetClaimSetByIdQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetClaimSetByIdV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenExists_ReturnsClaimSet()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();
        var result = new GetClaimSetByIdQuery(ctx).Execute(cs.ClaimSetId);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("CS1");
        result.Id.ShouldBe(cs.ClaimSetId);
        result.IsEditable.ShouldBeTrue();
    }

    [Test]
    public void Execute_WhenEdfiPreset_IsEditableFalse()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "Preset", IsEdfiPreset = true, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();
        new GetClaimSetByIdQuery(ctx).Execute(cs.ClaimSetId).IsEditable.ShouldBeFalse();
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetClaimSetByIdQuery(ctx).Execute(9999));
    }
}
