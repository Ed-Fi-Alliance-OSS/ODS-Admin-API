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
public class AddOdsInstanceDerivativeCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddOdsInstanceDerivative_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsDerivative()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        var result = new AddOdsInstanceDerivativeCommand(ctx).Execute(new AddOdsInstanceDerivativeModelStub
        {
            OdsInstanceId = ods.OdsInstanceId, DerivativeType = "read-replica", ConnectionString = "cs2"
        });
        result.OdsInstanceDerivativeId.ShouldBeGreaterThan(0);
        result.DerivativeType.ShouldBe("read-replica");
        ctx.OdsInstanceDerivatives.Count().ShouldBe(1);
    }

    [Test]
    public void Execute_WhenOdsInstanceNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new AddOdsInstanceDerivativeCommand(ctx).Execute(new AddOdsInstanceDerivativeModelStub
        {
            OdsInstanceId = 9999, DerivativeType = "read-replica", ConnectionString = "cs"
        }));
    }

    private class AddOdsInstanceDerivativeModelStub : IAddOdsInstanceDerivativeModel
    {
        public int OdsInstanceId { get; set; }
        public string? DerivativeType { get; set; }
        public string? ConnectionString { get; set; }
    }
}
