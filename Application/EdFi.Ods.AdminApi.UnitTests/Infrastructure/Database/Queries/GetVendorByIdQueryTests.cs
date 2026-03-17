// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetVendorByIdQueryTests
{
    [Test]
    public void Execute_WithUnknownId_ReturnsNull()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetVendorByIdQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var query = new GetVendorByIdQuery(usersContext);

        query.Execute(999).ShouldBeNull();
    }

    [Test]
    public void Execute_WithExistingId_ReturnsVendor()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetVendorByIdQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor { VendorName = "Acme Vendor" };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var query = new GetVendorByIdQuery(usersContext);

        var result = query.Execute(vendor.VendorId);

        result.ShouldNotBeNull();
        result!.VendorId.ShouldBe(vendor.VendorId);
        result.VendorName.ShouldBe("Acme Vendor");
    }
}
