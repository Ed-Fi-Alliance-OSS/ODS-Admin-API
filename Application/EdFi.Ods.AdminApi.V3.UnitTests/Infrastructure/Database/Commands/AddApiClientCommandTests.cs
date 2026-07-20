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
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddApiClientCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddApiClientV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_PersistsApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        var result = new AddApiClientCommand(ctx).Execute(new AddApiClientModelStub { Name = "Client1", IsApproved = true, ApplicationId = app.ApplicationId }, DefaultOptions());
        result.Id.ShouldBeGreaterThan(0);
        result.Key.ShouldNotBeNullOrEmpty();
        ctx.ApiClients.Count().ShouldBe(1);
    }

    private class AddApiClientModelStub : IAddApiClientModel
    {
        public string Name { get; init; } = string.Empty;
        public bool IsApproved { get; init; }
        public int ApplicationId { get; init; }
        public IEnumerable<int>? DataStoreIds => null;
    }
}
