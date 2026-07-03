// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetApiClientsByApplicationIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApiClientsByAppV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_ReturnsClientsForApp()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "C1", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "C2", Application = app });
        ctx.SaveChanges();
        var result = new GetApiClientsByApplicationIdQuery(ctx).Execute(app.ApplicationId);
        result.Count.ShouldBe(2);
    }

    [Test]
    public void Execute_WhenNoClients_ReturnsEmpty()
    {
        using var ctx = CreateContext();
        new GetApiClientsByApplicationIdQuery(ctx).Execute(9999).ShouldBeEmpty();
    }
}
