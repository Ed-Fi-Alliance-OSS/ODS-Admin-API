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
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditOdsInstanceContextCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditOdsInstanceContext_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesContextValue()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        var oic = new OdsInstanceContext { OdsInstance = ods, ContextKey = "key", ContextValue = "old" };
        ctx.OdsInstanceContexts.Add(oic);
        ctx.SaveChanges();
        new EditOdsInstanceContextCommand(ctx).Execute(new EditOdsInstanceContextModelStub { Id = oic.OdsInstanceContextId, OdsInstanceId = ods.OdsInstanceId, ContextKey = "key", ContextValue = "new" });
        ctx.OdsInstanceContexts.Single().ContextValue.ShouldBe("new");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditOdsInstanceContextCommand(ctx).Execute(new EditOdsInstanceContextModelStub { Id = 9999, OdsInstanceId = 1, ContextKey = "k", ContextValue = "v" }));
    }

    private class EditOdsInstanceContextModelStub : IEditOdsInstanceContextModel
    {
        public int Id { get; set; }
        public int OdsInstanceId { get; set; }
        public string? ContextKey { get; set; }
        public string? ContextValue { get; set; }
    }
}
