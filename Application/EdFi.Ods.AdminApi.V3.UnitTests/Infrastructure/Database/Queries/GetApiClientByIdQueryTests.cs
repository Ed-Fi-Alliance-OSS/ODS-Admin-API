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
public class GetApiClientByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApiClientByIdV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenExists_ReturnsApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS1", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "Client1", Application = app };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();
        var result = new GetApiClientByIdQuery(ctx).Execute(client.ApiClientId);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Client1");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetApiClientByIdQuery(ctx).Execute(9999));
    }
}
