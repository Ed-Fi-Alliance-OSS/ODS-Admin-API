// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationsByVendorIdQueryTests_{Guid.NewGuid()}")
            .Options);

    private static Application CreateApplication(Vendor vendor, string applicationName)
    {
        return new Application
        {
            ApplicationName = applicationName,
            ClaimSetName = $"{applicationName}ClaimSet",
            OperationalContextUri = "uri",
            Vendor = vendor,
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>(),
            ApiClients = new List<ApiClient>()
        };
    }

    [Test]
    public void Execute_WithMatchingVendorId_ReturnsApplications()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var otherVendor = new Vendor { VendorName = "Other Vendor" };
        context.Applications.AddRange(
            CreateApplication(vendor, "App One"),
            CreateApplication(vendor, "App Two"),
            CreateApplication(otherVendor, "App Three"));
        context.SaveChanges();

        var query = new GetApplicationsByVendorIdQuery(context);

        var result = query.Execute(vendor.VendorId);

        result.Count.ShouldBe(2);
        result.Select(x => x.ApplicationName).ShouldBe(["App One", "App Two"], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithExistingVendorAndNoApplications_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        context.Vendors.Add(vendor);
        context.SaveChanges();

        var query = new GetApplicationsByVendorIdQuery(context);

        var result = query.Execute(vendor.VendorId);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Execute_WithUnknownVendor_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var query = new GetApplicationsByVendorIdQuery(context);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }
}
