// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V3.Infrastructure.Database.Queries;

[TestFixture]
public class GetVendorsQueryTests
{
    [Test]
    public void Execute_ReturnsOnlyNonReservedVendors()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetVendorsQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        usersContext.Vendors.Add(new Vendor { VendorName = VendorExtensions.ReservedNames[0] });
        usersContext.Vendors.Add(new Vendor { VendorName = "Acme Vendor" });
        usersContext.SaveChanges();

        var options = Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
        var query = new GetVendorsQuery(usersContext, options);

        var result = query.Execute();

        result.Any(v => v.VendorName == "Acme Vendor").ShouldBeTrue();
        result.Any(v => v.VendorName == VendorExtensions.ReservedNames[0]).ShouldBeFalse();
    }

    [Test]
    public void Execute_WithCompanyFilter_ReturnsMatchingVendor()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetVendorsQuery_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        usersContext.Vendors.Add(new Vendor
        {
            VendorName = "Acme Vendor",
            Users = new List<User> { new User { FullName = "Acme User", Email = "acme@test.org" } }
        });
        usersContext.Vendors.Add(new Vendor
        {
            VendorName = "Other Vendor",
            Users = new List<User> { new User { FullName = "Other User", Email = "other@test.org" } }
        });
        usersContext.SaveChanges();

        var options = Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
        var query = new GetVendorsQuery(usersContext, options);

        var result = query.Execute(new CommonQueryParams(0, 25), null, "Acme Vendor", null, null, null);

        result.Count.ShouldBe(1);
        result.Single().VendorName.ShouldBe("Acme Vendor");
    }
}
