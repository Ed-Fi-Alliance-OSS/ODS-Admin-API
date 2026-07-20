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
public class GetApiClientOdsInstanceQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApiClientOdsInstanceQueryTests_{Guid.NewGuid()}")
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
    public void Execute_WithExistingMapping_ReturnsApiClientOdsInstance()
    {
        using var context = CreateContext();
        var application = CreateApplication(new Vendor { VendorName = "Acme Vendor" });
        var apiClient = new ApiClient(true)
        {
            Name = "Client One",
            Application = application,
            User = new User { FullName = "Client User", Email = "client@test.org" }
        };
        var odsInstance = new OdsInstance { Name = "Sandbox", InstanceType = "type", ConnectionString = "cs" };
        application.ApiClients.Add(apiClient);
        context.ApiClientOdsInstances.Add(new ApiClientOdsInstance { ApiClient = apiClient, OdsInstance = odsInstance });
        context.SaveChanges();

        var query = new GetApiClientOdsInstanceQuery(context);

        var result = query.Execute(apiClient.ApiClientId, odsInstance.OdsInstanceId);

        result.ShouldNotBeNull();
        result.ApiClient.ApiClientId.ShouldBe(apiClient.ApiClientId);
        result.OdsInstance.OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);
    }

    [Test]
    public void Execute_WithMissingMapping_ReturnsNull()
    {
        using var context = CreateContext();
        var query = new GetApiClientOdsInstanceQuery(context);

        var result = query.Execute(1, 1);

        result.ShouldBeNull();
    }
}
