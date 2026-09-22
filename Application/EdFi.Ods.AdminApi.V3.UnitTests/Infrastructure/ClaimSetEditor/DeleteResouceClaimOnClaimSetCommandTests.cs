// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;
using ResourceClaim = EdFi.Security.DataAccess.Models.ResourceClaim;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class DeleteResouceClaimOnClaimSetCommandTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteResourceClaimOnClaimSet_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_RemovesMatchingClaimSetResourceClaimActions()
    {
        using var ctx = CreateContext();
        var claimSet = new ClaimSet { ClaimSetName = "CS1" };
        var resourceClaim = new ResourceClaim { ResourceName = "Resource1", ClaimName = "Claim1" };
        ctx.ClaimSets.Add(claimSet);
        ctx.ResourceClaims.Add(resourceClaim);
        ctx.SaveChanges();
        ctx.ClaimSetResourceClaimActions.Add(new ClaimSetResourceClaimAction
        {
            ClaimSetId = claimSet.ClaimSetId,
            ResourceClaimId = resourceClaim.ResourceClaimId,
            ActionId = 1
        });
        ctx.SaveChanges();

        new DeleteResouceClaimOnClaimSetCommand(ctx, A.Fake<IDeletedEntityAuditCapture>())
            .Execute(claimSet.ClaimSetId, resourceClaim.ResourceClaimId);

        ctx.ClaimSetResourceClaimActions.Any().ShouldBeFalse();
    }

    [Test]
    public void Execute_RecordsFirstMatchingRowForAuditCapture()
    {
        using var ctx = CreateContext();
        var claimSet = new ClaimSet { ClaimSetName = "CS1" };
        var resourceClaim = new ResourceClaim { ResourceName = "Resource1", ClaimName = "Claim1" };
        ctx.ClaimSets.Add(claimSet);
        ctx.ResourceClaims.Add(resourceClaim);
        ctx.SaveChanges();
        ctx.ClaimSetResourceClaimActions.Add(new ClaimSetResourceClaimAction
        {
            ClaimSetId = claimSet.ClaimSetId,
            ResourceClaimId = resourceClaim.ResourceClaimId,
            ActionId = 1
        });
        ctx.SaveChanges();
        var capture = A.Fake<IDeletedEntityAuditCapture>();

        new DeleteResouceClaimOnClaimSetCommand(ctx, capture)
            .Execute(claimSet.ClaimSetId, resourceClaim.ResourceClaimId);

        A.CallTo(() => capture.Record(
            A<object>.That.Matches(o =>
                ((ClaimSetResourceClaimAction)o).ClaimSetId == claimSet.ClaimSetId
                && ((ClaimSetResourceClaimAction)o).ResourceClaimId == resourceClaim.ResourceClaimId)))
            .MustHaveHappenedOnceExactly();
    }
}
