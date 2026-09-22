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
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class DeleteApplicationCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteApp_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_DeletesApplicationAndApiClients()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "C1", Application = app });
        ctx.SaveChanges();

        new DeleteApplicationCommand(ctx, A.Fake<IDeletedEntityAuditCapture>()).Execute(app.ApplicationId);

        ctx.Applications.Count().ShouldBe(0);
        ctx.ApiClients.Count().ShouldBe(0);
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new DeleteApplicationCommand(ctx, A.Fake<IDeletedEntityAuditCapture>()).Execute(9999));
    }

    [Test]
    public void Execute_RecordsDeletedApplicationForAuditCapture()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        var capture = A.Fake<IDeletedEntityAuditCapture>();

        new DeleteApplicationCommand(ctx, capture).Execute(app.ApplicationId);

        A.CallTo(() => capture.Record(
            A<object>.That.Matches(o => ((Application)o).ApplicationId == app.ApplicationId)))
            .MustHaveHappenedOnceExactly();
    }
}
