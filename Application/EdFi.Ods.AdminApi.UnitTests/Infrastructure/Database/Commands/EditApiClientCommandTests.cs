// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Collections.Generic;
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
public class EditApiClientCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditApiClient_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesApiClientName()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "OldName", Application = app };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();
        new EditApiClientCommand(ctx).Execute(new EditApiClientModelStub { Id = client.ApiClientId, Name = "NewName", IsApproved = true, ApplicationId = app.ApplicationId });
        ctx.ApiClients.Single().Name.ShouldBe("NewName");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditApiClientCommand(ctx).Execute(new EditApiClientModelStub { Id = 9999, Name = "X", IsApproved = true, ApplicationId = 1 }));
    }

    private class EditApiClientModelStub : IEditApiClientModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsApproved { get; init; }
        public int ApplicationId { get; init; }
        public IEnumerable<int>? OdsInstanceIds => null;
    }
}
