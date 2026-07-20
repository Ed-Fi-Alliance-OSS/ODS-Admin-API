// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetAllApplicationsQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetAllApplicationsQueryTests_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    private static Application CreateApplication(string name, string claimSetName, Vendor vendor)
    {
        return new Application
        {
            ApplicationName = name,
            ClaimSetName = claimSetName,
            OperationalContextUri = "uri",
            Vendor = vendor,
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>(),
            ApiClients = new List<ApiClient>()
        };
    }

    [Test]
    public void Execute_WithoutFilters_ReturnsAllApplications()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        context.Applications.AddRange(
            CreateApplication("App One", "ClaimSetA", vendor),
            CreateApplication("App Two", "ClaimSetB", vendor));
        context.SaveChanges();

        var query = new GetAllApplicationsQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, null, null, null);

        result.Count.ShouldBe(2);
        result.Select(x => x.ApplicationName).ShouldBe(["App One", "App Two"], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithApplicationNameFilter_ReturnsMatchingApplication()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        context.Applications.AddRange(
            CreateApplication("App One", "ClaimSetA", vendor),
            CreateApplication("App Two", "ClaimSetB", vendor));
        context.SaveChanges();

        var query = new GetAllApplicationsQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, "App Two", null, null);

        result.Count.ShouldBe(1);
        result.Single().ApplicationName.ShouldBe("App Two");
    }

    [Test]
    public void Execute_WithClaimSetFilter_ReturnsMatchingApplication()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        context.Applications.AddRange(
            CreateApplication("App One", "ClaimSetA", vendor),
            CreateApplication("App Two", "ClaimSetB", vendor));
        context.SaveChanges();

        var query = new GetAllApplicationsQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, null, "ClaimSetB", null);

        result.Count.ShouldBe(1);
        result.Single().ClaimSetName.ShouldBe("ClaimSetB");
    }

    [Test]
    public void Execute_WithIdFilter_ReturnsMatchingApplication()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var applicationOne = CreateApplication("App One", "ClaimSetA", vendor);
        var applicationTwo = CreateApplication("App Two", "ClaimSetB", vendor);
        context.Applications.AddRange(applicationOne, applicationTwo);
        context.SaveChanges();

        var query = new GetAllApplicationsQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), applicationTwo.ApplicationId, null, null, null);

        result.Count.ShouldBe(1);
        result.Single().ApplicationId.ShouldBe(applicationTwo.ApplicationId);
    }
}
