// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetClaimSetByIdQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetClaimSetById_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenClaimSetExists_ReturnsClaimSet()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "TestSet", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();

        var result = new GetClaimSetByIdQuery(ctx).Execute(cs.ClaimSetId);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("TestSet");
        result.IsEditable.ShouldBeTrue();
    }

    [Test]
    public void Execute_WhenClaimSetIsEdfiPreset_IsEditableFalse()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "EdFiPreset", IsEdfiPreset = true, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();

        var result = new GetClaimSetByIdQuery(ctx).Execute(cs.ClaimSetId);

        result.IsEditable.ShouldBeFalse();
    }

    [Test]
    public void Execute_WhenClaimSetIsForApplicationUseOnly_IsEditableFalse()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "AppOnly", IsEdfiPreset = false, ForApplicationUseOnly = true };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();

        var result = new GetClaimSetByIdQuery(ctx).Execute(cs.ClaimSetId);

        result.IsEditable.ShouldBeFalse();
    }

    [Test]
    public void Execute_WhenClaimSetNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetClaimSetByIdQuery(ctx).Execute(999));
    }
}
