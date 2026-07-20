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
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditDataStoreCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditDataStore_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesDataStore()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        new EditDataStoreCommand(ctx).Execute(new EditDataStoreModelStub { Id = ods.OdsInstanceId, Name = "Updated", DataStoreType = "type2" });
        ctx.OdsInstances.Single().Name.ShouldBe("Updated");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditDataStoreCommand(ctx).Execute(new EditDataStoreModelStub { Id = 9999, Name = "X" }));
    }

    private class EditDataStoreModelStub : IEditDataStoreModel
    {
        public int Id { get; set; }
        public string? Name { get; init; }
        public string? DataStoreType { get; init; }
        public string? ConnectionString { get; init; }
    }
}
