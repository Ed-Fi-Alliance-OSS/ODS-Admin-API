// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using SecurityClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class DeleteClaimSetCommandTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteClaimSet_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_DeletesClaimSet()
    {
        using var ctx = CreateContext();
        var cs = new SecurityClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();
        new DeleteClaimSetCommand(ctx, A.Fake<IDeletedEntityAuditCapture>())
            .Execute(new DeleteClaimSetModelStub { Id = cs.ClaimSetId, Name = "CS1" });
        ctx.ClaimSets.Count().ShouldBe(0);
    }

    [Test]
    public void Execute_WhenSystemReserved_ThrowsAdminApiException()
    {
        using var ctx = CreateContext();
        var cs = new SecurityClaimSet { ClaimSetName = "SystemCS", IsEdfiPreset = true, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();
        Should.Throw<AdminApiException>(() => new DeleteClaimSetCommand(ctx, A.Fake<IDeletedEntityAuditCapture>())
            .Execute(new DeleteClaimSetModelStub { Id = cs.ClaimSetId, Name = "SystemCS" }));
    }

    [Test]
    public void Execute_RecordsDeletedClaimSetForAuditCapture()
    {
        using var ctx = CreateContext();
        var cs = new SecurityClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();
        var capture = A.Fake<IDeletedEntityAuditCapture>();

        new DeleteClaimSetCommand(ctx, capture).Execute(new DeleteClaimSetModelStub { Id = cs.ClaimSetId, Name = "CS1" });

        A.CallTo(() => capture.Record(
            A<object>.That.Matches(o => ((SecurityClaimSet)o).ClaimSetId == cs.ClaimSetId)))
            .MustHaveHappenedOnceExactly();
    }

    private class DeleteClaimSetModelStub : IDeleteClaimSetModel
    {
        public string? Name { get; init; }
        public int Id { get; init; }
    }
}
