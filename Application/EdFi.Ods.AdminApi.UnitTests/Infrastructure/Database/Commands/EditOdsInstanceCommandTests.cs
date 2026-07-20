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
public class EditOdsInstanceCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditOdsInstance_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesOdsInstance()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        new EditOdsInstanceCommand(ctx).Execute(new EditOdsInstanceModelStub { Id = ods.OdsInstanceId, Name = "Updated", InstanceType = "type2" });
        ctx.OdsInstances.Single().Name.ShouldBe("Updated");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditOdsInstanceCommand(ctx).Execute(new EditOdsInstanceModelStub { Id = 9999, Name = "X" }));
    }

    private class EditOdsInstanceModelStub : IEditOdsInstanceModel
    {
        public int Id { get; set; }
        public string? Name { get; init; }
        public string? InstanceType { get; init; }
        public string? ConnectionString { get; init; }
    }
}
