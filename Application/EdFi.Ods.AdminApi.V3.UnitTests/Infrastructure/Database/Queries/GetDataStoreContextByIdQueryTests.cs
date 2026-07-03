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
public class GetDataStoreContextByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreContextById_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenExists_ReturnsDataStoreContext()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        var oic = new OdsInstanceContext { OdsInstance = ods, ContextKey = "key", ContextValue = "val" };
        ctx.OdsInstanceContexts.Add(oic);
        ctx.SaveChanges();
        var result = new GetDataStoreContextByIdQuery(ctx).Execute(oic.OdsInstanceContextId);
        result.ShouldNotBeNull();
        result.ContextKey.ShouldBe("key");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetDataStoreContextByIdQuery(ctx).Execute(9999));
    }
}
