// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreDerivativeByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreDerivativeById_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenExists_ReturnsDataStoreDerivative()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        var d = new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = "cs-replica" };
        ctx.OdsInstanceDerivatives.Add(d);
        ctx.SaveChanges();
        var result = new GetDataStoreDerivativeByIdQuery(ctx).Execute(d.OdsInstanceDerivativeId);
        result.ShouldNotBeNull();
        result.DerivativeType.ShouldBe("read-replica");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetDataStoreDerivativeByIdQuery(ctx).Execute(9999));
    }
}
