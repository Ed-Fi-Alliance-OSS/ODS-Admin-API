// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceIdsByApplicationIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceIdsByApplicationIdQueryTests_{Guid.NewGuid()}")
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

    private static ApiClient CreateApiClient(Application application, string name)
    {
        var apiClient = new ApiClient(true)
        {
            Name = name,
            Application = application,
            User = new User { FullName = $"{name} User", Email = $"{name.ToLowerInvariant()}@test.org" }
        };
        application.ApiClients.Add(apiClient);

        return apiClient;
    }

    [Test]
    public void Execute_WithApplicationId_ReturnsDistinctMappedOdsInstanceIds()
    {
        using var context = CreateContext();
        var application = CreateApplication(new Vendor { VendorName = "Acme Vendor" }, "App One");
        var firstClient = CreateApiClient(application, "clientone");
        var secondClient = CreateApiClient(application, "clienttwo");
        var odsOne = new OdsInstance { Name = "Sandbox A", InstanceType = "type", ConnectionString = "cs1" };
        var odsTwo = new OdsInstance { Name = "Sandbox B", InstanceType = "type", ConnectionString = "cs2" };
        context.ApiClientOdsInstances.AddRange(
            new ApiClientOdsInstance { ApiClient = firstClient, OdsInstance = odsOne },
            new ApiClientOdsInstance { ApiClient = secondClient, OdsInstance = odsOne },
            new ApiClientOdsInstance { ApiClient = secondClient, OdsInstance = odsTwo });
        context.SaveChanges();

        var query = new GetOdsInstanceIdsByApplicationIdQuery(context);

        var result = query.Execute(application.ApplicationId);

        result.ShouldBe([odsOne.OdsInstanceId, odsTwo.OdsInstanceId], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithUnknownApplicationId_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var query = new GetOdsInstanceIdsByApplicationIdQuery(context);

        var result = query.Execute(999);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Execute_WithApplicationIds_ReturnsGroupedMappings()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var firstApplication = CreateApplication(vendor, "App One");
        var secondApplication = CreateApplication(vendor, "App Two");
        var firstClient = CreateApiClient(firstApplication, "clientone");
        var secondClient = CreateApiClient(secondApplication, "clienttwo");
        var odsOne = new OdsInstance { Name = "Sandbox A", InstanceType = "type", ConnectionString = "cs1" };
        var odsTwo = new OdsInstance { Name = "Sandbox B", InstanceType = "type", ConnectionString = "cs2" };
        context.ApiClientOdsInstances.AddRange(
            new ApiClientOdsInstance { ApiClient = firstClient, OdsInstance = odsOne },
            new ApiClientOdsInstance { ApiClient = secondClient, OdsInstance = odsTwo });
        context.SaveChanges();

        var query = new GetOdsInstanceIdsByApplicationIdQuery(context);

        var result = query.Execute(new[] { firstApplication.ApplicationId, secondApplication.ApplicationId });

        result.Count.ShouldBe(2);
        result[firstApplication.ApplicationId].Single().ShouldBe(odsOne.OdsInstanceId);
        result[secondApplication.ApplicationId].Single().ShouldBe(odsTwo.OdsInstanceId);
    }
}
