// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetApplicationsByVendorIdQueryTests
{
    [Test]
    public void Execute_WithUnknownVendorId_ThrowsNotFoundException()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationsByVendorIdQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var query = new GetApplicationsByVendorIdQuery(usersContext);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }

    [Test]
    public void Execute_WithExistingVendorAndNoApplications_ReturnsEmptyList()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationsByVendorIdQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor { VendorName = "Acme Vendor" };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var query = new GetApplicationsByVendorIdQuery(usersContext);

        var result = query.Execute(vendor.VendorId);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Execute_WithExistingVendorAndApplications_ReturnsApplications()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationsByVendorIdQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var application = new Application
        {
            ApplicationName = "Acme App",
            ClaimSetName = "ClaimSet",
            OperationalContextUri = "uri://ed-fi.org",
            Vendor = vendor
        };
        usersContext.Vendors.Add(vendor);
        usersContext.Applications.Add(application);
        usersContext.SaveChanges();

        var query = new GetApplicationsByVendorIdQuery(usersContext);

        var result = query.Execute(vendor.VendorId);

        result.Count.ShouldBe(1);
        result[0].ApplicationName.ShouldBe("Acme App");
    }
}
