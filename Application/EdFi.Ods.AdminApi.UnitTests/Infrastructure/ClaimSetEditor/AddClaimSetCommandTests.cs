// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class AddClaimSetCommandTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"AddClaimSet_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsClaimSet()
    {
        using var ctx = CreateContext();
        var id = new AddClaimSetCommand(ctx).Execute(new AddClaimSetModelStub { ClaimSetName = "NewCS" });
        id.ShouldBeGreaterThan(0);
        ctx.ClaimSets.Count().ShouldBe(1);
        ctx.ClaimSets.Single().ClaimSetName.ShouldBe("NewCS");
        ctx.ClaimSets.Single().IsEdfiPreset.ShouldBeFalse();
        ctx.ClaimSets.Single().ForApplicationUseOnly.ShouldBeFalse();
    }

    private class AddClaimSetModelStub : IAddClaimSetModel
    {
        public string? ClaimSetName { get; init; }
    }
}
