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
public class DeleteApiClientCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteApiClientV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_DeletesApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "C1", Application = app };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();
        new DeleteApiClientCommand(ctx, A.Fake<IDeletedEntityAuditCapture>()).Execute(client.ApiClientId);
        ctx.ApiClients.Count().ShouldBe(0);
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new DeleteApiClientCommand(ctx, A.Fake<IDeletedEntityAuditCapture>()).Execute(9999));
    }

    [Test]
    public void Execute_RecordsDeletedApiClientForAuditCapture()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "C1", Application = app };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();
        var capture = A.Fake<IDeletedEntityAuditCapture>();

        new DeleteApiClientCommand(ctx, capture).Execute(client.ApiClientId);

        A.CallTo(() => capture.Record(
            A<object>.That.Matches(o => ((ApiClient)o).ApiClientId == client.ApiClientId)))
            .MustHaveHappenedOnceExactly();
    }
}
