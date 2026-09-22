// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class DeleteDataStoreContextCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteDataStoreContext_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_DeletesDataStoreContext()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        var oic = new OdsInstanceContext { OdsInstance = ods, ContextKey = "k", ContextValue = "v" };
        ctx.OdsInstanceContexts.Add(oic);
        ctx.SaveChanges();
        new DeleteDataStoreContextCommand(ctx, A.Fake<IDeletedEntityAuditCapture>()).Execute(oic.OdsInstanceContextId);
        ctx.OdsInstanceContexts.Count().ShouldBe(0);
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new DeleteDataStoreContextCommand(ctx, A.Fake<IDeletedEntityAuditCapture>()).Execute(9999));
    }

    [Test]
    public void Execute_RecordsDeletedDataStoreContextForAuditCapture()
    {
        using var ctx = CreateContext();
        var instance = new OdsInstance { Name = "ODS1", InstanceType = "Ods", ConnectionString = "cs" };
        ctx.OdsInstances.Add(instance);
        var context = new OdsInstanceContext { OdsInstance = instance, ContextKey = "ContextKey1", ContextValue = "ContextValue1" };
        ctx.OdsInstanceContexts.Add(context);
        ctx.SaveChanges();
        var capture = A.Fake<IDeletedEntityAuditCapture>();

        new DeleteDataStoreContextCommand(ctx, capture).Execute(context.OdsInstanceContextId);

        A.CallTo(() => capture.Record(
            A<object>.That.Matches(o => ((OdsInstanceContext)o).OdsInstanceContextId == context.OdsInstanceContextId)))
            .MustHaveHappenedOnceExactly();
    }
}
