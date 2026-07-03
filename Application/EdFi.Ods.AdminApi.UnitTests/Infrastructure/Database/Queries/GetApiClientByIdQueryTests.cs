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
public class GetApiClientByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApiClientByIdQueryTests_{Guid.NewGuid()}")
            .Options);

    private static Application CreateApplication(Vendor vendor)
    {
        return new Application
        {
            ApplicationName = "Test App",
            ClaimSetName = "ClaimSet",
            OperationalContextUri = "uri",
            Vendor = vendor,
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>(),
            ApiClients = new List<ApiClient>()
        };
    }

    [Test]
    public void Execute_WithExistingId_ReturnsApiClient()
    {
        using var context = CreateContext();
        var application = CreateApplication(new Vendor { VendorName = "Acme Vendor" });
        var apiClient = new ApiClient(true)
        {
            Name = "Client One",
            Application = application,
            User = new User { FullName = "Client User", Email = "client@test.org" }
        };
        application.ApiClients.Add(apiClient);
        context.ApiClients.Add(apiClient);
        context.SaveChanges();

        var query = new GetApiClientByIdQuery(context);

        var result = query.Execute(apiClient.ApiClientId);

        result.Name.ShouldBe("Client One");
        result.Application.ShouldNotBeNull();
        result.Application.ApplicationName.ShouldBe("Test App");
    }

    [Test]
    public void Execute_WithUnknownId_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var query = new GetApiClientByIdQuery(context);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }
}
