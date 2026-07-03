// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using Action = EdFi.Security.DataAccess.Models.Action;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;
using SecurityResourceClaim = EdFi.Security.DataAccess.Models.ResourceClaim;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetResourcesByClaimSetIdQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetResourcesByCSV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void AllResources_WhenNoResources_ReturnsEmpty()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "EmptyCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();
        new GetResourcesByClaimSetIdQuery(ctx).AllResources(cs.ClaimSetId).ShouldBeEmpty();
    }

    [Test]
    public void AllResources_WhenParentResourceExists_ReturnsParent()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        var action = new Action { ActionName = "Read", ActionUri = "uri" };
        ctx.Actions.Add(action);
        var rc = new SecurityResourceClaim { ResourceName = "schools", ClaimName = "schools" };
        ctx.ResourceClaims.Add(rc);
        ctx.SaveChanges();
        ctx.ClaimSetResourceClaimActions.Add(new ClaimSetResourceClaimAction { ClaimSet = cs, ResourceClaim = rc, Action = action });
        ctx.SaveChanges();
        var result = new GetResourcesByClaimSetIdQuery(ctx).AllResources(cs.ClaimSetId);
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("schools");
    }

    [Test]
    public void AddChildResourcesToParents_WhenChildHasKnownParent_AddsToParent()
    {
        var parent = new EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.ResourceClaim { Id = 1, Name = "parent" };
        var child = new EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.ResourceClaim { Id = 2, Name = "child", ParentId = 1 };
        var parents = new System.Collections.Generic.List<EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.ResourceClaim> { parent };
        var children = new System.Collections.Generic.List<EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.ResourceClaim> { child };
        GetResourcesByClaimSetIdQuery.AddChildResourcesToParents(children, parents);
        parents[0].Children.Count.ShouldBe(1);
        parents[0].Children[0].Name.ShouldBe("child");
    }
}
