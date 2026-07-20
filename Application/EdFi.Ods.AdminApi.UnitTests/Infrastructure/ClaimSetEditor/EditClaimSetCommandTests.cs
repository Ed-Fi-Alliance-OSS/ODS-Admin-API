// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using SecurityClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class EditClaimSetCommandTests
{
    private static SqlServerSecurityContext CreateSecurityContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"EditClaimSet_sec_{Guid.NewGuid()}")
            .Options);
    private static SqlServerUsersContext CreateUsersContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditClaimSet_usr_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesClaimSetName()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();
        var cs = new SecurityClaimSet { ClaimSetName = "OldName", IsEdfiPreset = false, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();
        new EditClaimSetCommand(secCtx, usrCtx).Execute(new EditClaimSetModelStub { ClaimSetId = cs.ClaimSetId, ClaimSetName = "NewName" });
        secCtx.ClaimSets.Single().ClaimSetName.ShouldBe("NewName");
    }

    [Test]
    public void Execute_WhenSystemReserved_ThrowsAdminApiException()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();
        var cs = new SecurityClaimSet { ClaimSetName = "SysCS", IsEdfiPreset = true, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();
        Should.Throw<AdminApiException>(() => new EditClaimSetCommand(secCtx, usrCtx).Execute(new EditClaimSetModelStub { ClaimSetId = cs.ClaimSetId, ClaimSetName = "X" }));
    }

    private class EditClaimSetModelStub : IEditClaimSetModel
    {
        public string? ClaimSetName { get; init; }
        public int ClaimSetId { get; init; }
    }
}
