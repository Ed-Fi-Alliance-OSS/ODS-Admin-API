#nullable enable
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApp.Management.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class CopyClaimSetCommandTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"CopyClaimSet_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_CopiesClaimSetWithNewName()
    {
        using var ctx = CreateContext();
        var source = new ClaimSet { ClaimSetName = "SourceCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(source);
        ctx.SaveChanges();

        var newId = new CopyClaimSetCommand(ctx).Execute(new CopyClaimSetModelStub { OriginalId = source.ClaimSetId, Name = "CopiedCS" });

        newId.ShouldBeGreaterThan(0);
        ctx.ClaimSets.Count().ShouldBe(2);
        ctx.ClaimSets.Single(c => c.ClaimSetId == newId).ClaimSetName.ShouldBe("CopiedCS");
    }

    private class CopyClaimSetModelStub : ICopyClaimSetModel
    {
        public int OriginalId { get; init; }
        public string? Name { get; init; }
    }
}
