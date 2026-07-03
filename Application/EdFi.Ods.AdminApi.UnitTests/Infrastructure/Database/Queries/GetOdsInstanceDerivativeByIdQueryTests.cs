// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceDerivativeByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceDerivativeByIdQueryTests_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WithExistingId_ReturnsOdsInstanceDerivative()
    {
        using var context = CreateContext();
        var derivative = new OdsInstanceDerivative
        {
            OdsInstance = new OdsInstance { Name = "Sandbox", InstanceType = "type", ConnectionString = "cs" },
            DerivativeType = "read-replica",
            ConnectionString = "derivative-cs"
        };
        context.OdsInstanceDerivatives.Add(derivative);
        context.SaveChanges();

        var query = new GetOdsInstanceDerivativeByIdQuery(context);

        var result = query.Execute(derivative.OdsInstanceDerivativeId);

        result.DerivativeType.ShouldBe("read-replica");
        result.OdsInstance.ShouldNotBeNull();
    }

    [Test]
    public void Execute_WithUnknownId_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var query = new GetOdsInstanceDerivativeByIdQuery(context);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }
}
