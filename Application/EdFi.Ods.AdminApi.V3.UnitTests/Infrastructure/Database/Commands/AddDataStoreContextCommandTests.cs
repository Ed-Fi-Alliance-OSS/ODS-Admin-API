// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddDataStoreContextCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddDataStoreContext_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsDataStoreContext()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        var result = new AddDataStoreContextCommand(ctx).Execute(new AddDataStoreContextModelStub { DataStoreId = ods.OdsInstanceId, ContextKey = "key", ContextValue = "val" });
        result.OdsInstanceContextId.ShouldBeGreaterThan(0);
        ctx.OdsInstanceContexts.Count().ShouldBe(1);
    }

    private class AddDataStoreContextModelStub : IAddDataStoreContextModel
    {
        public int DataStoreId { get; set; }
        public string? ContextKey { get; set; }
        public string? ContextValue { get; set; }
    }
}
