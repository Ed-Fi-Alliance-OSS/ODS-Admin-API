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
public class GetApplicationsByDataStoreIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetAppsByDataStore_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenDataStoreExistsWithApps_ReturnsApplications()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "C1", Application = app };
        ctx.ApiClients.Add(client);
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.ApiClientOdsInstances.Add(new ApiClientOdsInstance { ApiClient = client, OdsInstance = ods });
        ctx.SaveChanges();
        var result = new GetApplicationsByDataStoreIdQuery(ctx).Execute(ods.OdsInstanceId);
        result.Count.ShouldBe(1);
        result[0].ApplicationName.ShouldBe("App1");
    }

    [Test]
    public void Execute_WhenDataStoreNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetApplicationsByDataStoreIdQuery(ctx).Execute(9999));
    }
}
