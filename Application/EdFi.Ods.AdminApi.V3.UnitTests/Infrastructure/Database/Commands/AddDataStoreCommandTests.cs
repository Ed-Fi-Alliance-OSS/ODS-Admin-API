// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddDataStoreCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddDataStore_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsDataStore()
    {
        using var ctx = CreateContext();
        var result = new AddDataStoreCommand(ctx).Execute(new AddDataStoreModelStub { Name = "DS1", DataStoreType = "type", ConnectionString = "cs" });
        result.OdsInstanceId.ShouldBeGreaterThan(0);
        result.Name.ShouldBe("DS1");
        ctx.OdsInstances.Count().ShouldBe(1);
    }

    private class AddDataStoreModelStub : IAddDataStoreModel
    {
        public string? Name { get; init; }
        public string? DataStoreType { get; init; }
        public string? ConnectionString { get; init; }
    }
}
