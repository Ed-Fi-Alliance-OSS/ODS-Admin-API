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
public class AddDataStoreDerivativeCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddDataStoreDerivative_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsDerivative()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        var result = new AddDataStoreDerivativeCommand(ctx).Execute(new AddDataStoreDerivativeModelStub { DataStoreId = ods.OdsInstanceId, DerivativeType = "replica", ConnectionString = "cs2" });
        result.OdsInstanceDerivativeId.ShouldBeGreaterThan(0);
        ctx.OdsInstanceDerivatives.Count().ShouldBe(1);
    }

    [Test]
    public void Execute_WhenDataStoreNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new AddDataStoreDerivativeCommand(ctx).Execute(new AddDataStoreDerivativeModelStub { DataStoreId = 9999, DerivativeType = "replica", ConnectionString = "cs" }));
    }

    private class AddDataStoreDerivativeModelStub : IAddDataStoreDerivativeModel
    {
        public int DataStoreId { get; set; }
        public string? DerivativeType { get; set; }
        public string? ConnectionString { get; set; }
    }
}
