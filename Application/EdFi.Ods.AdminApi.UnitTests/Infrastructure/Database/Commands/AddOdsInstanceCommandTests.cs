// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddOdsInstanceCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddOdsInstance_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsOdsInstance()
    {
        using var ctx = CreateContext();
        var result = new AddOdsInstanceCommand(ctx).Execute(new AddOdsInstanceModelStub
        {
            Name = "ODS1", InstanceType = "type", ConnectionString = "cs"
        });
        result.OdsInstanceId.ShouldBeGreaterThan(0);
        result.Name.ShouldBe("ODS1");
        ctx.OdsInstances.Count().ShouldBe(1);
    }

    private class AddOdsInstanceModelStub : IAddOdsInstanceModel
    {
        public string? Name { get; init; }
        public string? InstanceType { get; init; }
        public string? ConnectionString { get; init; }
    }
}
