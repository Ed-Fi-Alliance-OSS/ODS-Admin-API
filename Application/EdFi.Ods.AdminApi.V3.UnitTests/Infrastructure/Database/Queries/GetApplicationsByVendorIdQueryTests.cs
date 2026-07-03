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
public class GetApplicationsByVendorIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetAppsByVendorV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_ReturnsApplicationsForVendor()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.Applications.Add(new Application { ApplicationName = "App1", ClaimSetName = "CS1", Vendor = vendor, OperationalContextUri = "uri" });
        ctx.SaveChanges();
        var result = new GetApplicationsByVendorIdQuery(ctx).Execute(vendor.VendorId);
        result.Count.ShouldBe(1);
        result[0].ApplicationName.ShouldBe("App1");
    }

    [Test]
    public void Execute_WhenVendorHasNoApps_ReturnsEmpty()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "EmptyVendor" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();
        var result = new GetApplicationsByVendorIdQuery(ctx).Execute(vendor.VendorId);
        result.ShouldBeEmpty();
    }
}

