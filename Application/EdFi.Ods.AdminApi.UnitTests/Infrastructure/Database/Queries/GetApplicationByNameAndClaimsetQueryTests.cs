// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetApplicationByNameAndClaimsetQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationByNameAndClaimsetQueryTests_{Guid.NewGuid()}")
            .Options);

    private static Application CreateApplication(Vendor vendor, string applicationName, string claimSetName)
    {
        return new Application
        {
            ApplicationName = applicationName,
            ClaimSetName = claimSetName,
            OperationalContextUri = "uri",
            Vendor = vendor,
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>(),
            ApiClients = new List<ApiClient>()
        };
    }

    [Test]
    public void Execute_WithMatchingNameAndClaimSet_ReturnsApplication()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        context.Applications.AddRange(
            CreateApplication(vendor, "App One", "ClaimSetA"),
            CreateApplication(vendor, "App Two", "ClaimSetB"));
        context.SaveChanges();

        var query = new GetApplicationByNameAndClaimsetQuery(context);

        var result = query.Execute("App Two", "ClaimSetB");

        result.ShouldNotBeNull();
        result.ApplicationName.ShouldBe("App Two");
    }

    [Test]
    public void Execute_WithUnknownNameAndClaimSet_ReturnsNull()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        context.Applications.Add(CreateApplication(vendor, "App One", "ClaimSetA"));
        context.SaveChanges();

        var query = new GetApplicationByNameAndClaimsetQuery(context);

        var result = query.Execute("Missing App", "ClaimSetZ");

        result.ShouldBeNull();
    }
}
