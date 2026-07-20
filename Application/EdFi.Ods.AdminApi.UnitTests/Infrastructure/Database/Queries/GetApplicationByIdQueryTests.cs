// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetApplicationByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationByIdQueryTests_{Guid.NewGuid()}")
            .Options);

    private static Application CreateApplication(Vendor vendor, string applicationName = "Test App", string claimSetName = "ClaimSet")
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
    public void Execute_WithExistingId_ReturnsApplication()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var application = CreateApplication(vendor);
        application.ApiClients.Add(new ApiClient(true)
        {
            Name = "Client One",
            Application = application,
            User = new User { FullName = "Client User", Email = "client@test.org" }
        });
        context.Applications.Add(application);
        context.SaveChanges();

        var query = new GetApplicationByIdQuery(context);

        var result = query.Execute(application.ApplicationId);

        result.ApplicationName.ShouldBe("Test App");
        result.Vendor.ShouldNotBeNull();
        result.ApiClients.Count.ShouldBe(1);
    }

    [Test]
    public void Execute_WithUnknownId_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var query = new GetApplicationByIdQuery(context);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }
}
