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
public class EditOdsInstanceDerivativeCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditOdsInstanceDerivative_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesDerivativeType()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        var d = new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "old", ConnectionString = "cs2" };
        ctx.OdsInstanceDerivatives.Add(d);
        ctx.SaveChanges();
        new EditOdsInstanceDerivativeCommand(ctx).Execute(new EditOdsInstanceDerivativeModelStub { Id = d.OdsInstanceDerivativeId, OdsInstanceId = ods.OdsInstanceId, DerivativeType = "new", ConnectionString = "cs3" });
        ctx.OdsInstanceDerivatives.Single().DerivativeType.ShouldBe("new");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditOdsInstanceDerivativeCommand(ctx).Execute(new EditOdsInstanceDerivativeModelStub { Id = 9999, OdsInstanceId = 1, DerivativeType = "x", ConnectionString = "cs" }));
    }

    private class EditOdsInstanceDerivativeModelStub : IEditOdsInstanceDerivativeModel
    {
        public int Id { get; set; }
        public int OdsInstanceId { get; set; }
        public string? DerivativeType { get; set; }
        public string? ConnectionString { get; set; }
    }
}
