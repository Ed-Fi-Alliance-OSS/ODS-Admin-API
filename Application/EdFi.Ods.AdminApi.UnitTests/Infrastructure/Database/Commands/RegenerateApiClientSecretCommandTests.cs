// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class RegenerateApiClientSecretCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"RegenerateSecret_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_RegeneratesSecretAndReturnsResult()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "C1", Application = app };
        var originalSecret = client.Secret;
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();

        var result = new RegenerateApiClientSecretCommand(ctx).Execute(client.ApiClientId);

        result.Id.ShouldBe(client.ApiClientId);
        result.Secret.ShouldNotBeNullOrEmpty();
        result.Secret.ShouldNotBe(originalSecret);
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new RegenerateApiClientSecretCommand(ctx).Execute(9999));
    }
}
