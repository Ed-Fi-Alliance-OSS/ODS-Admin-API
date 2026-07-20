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
public class GetApplicationsByOdsInstanceIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetApplicationsByOdsInstanceIdQueryTests_{Guid.NewGuid()}")
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
    public void Execute_WithMappedOdsInstance_ReturnsApplications()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var odsInstance = new OdsInstance { Name = "Sandbox", InstanceType = "type", ConnectionString = "cs" };
        var appOne = CreateApplication(vendor, "App One");
        var appTwo = CreateApplication(vendor, "App Two");
        var apiClientOne = CreateApiClient(appOne, "clientone");
        var apiClientTwo = CreateApiClient(appTwo, "clienttwo");
        context.ApiClientOdsInstances.AddRange(
            new ApiClientOdsInstance { ApiClient = apiClientOne, OdsInstance = odsInstance },
            new ApiClientOdsInstance { ApiClient = apiClientTwo, OdsInstance = odsInstance });
        context.SaveChanges();

        var query = new GetApplicationsByOdsInstanceIdQuery(context);

        var result = query.Execute(odsInstance.OdsInstanceId);

        result.Count.ShouldBe(2);
        result.Select(x => x.ApplicationName).ShouldBe(["App One", "App Two"], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithExistingOdsInstanceAndNoMappings_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var odsInstance = new OdsInstance { Name = "Sandbox", InstanceType = "type", ConnectionString = "cs" };
        context.OdsInstances.Add(odsInstance);
        context.SaveChanges();

        var query = new GetApplicationsByOdsInstanceIdQuery(context);

        var result = query.Execute(odsInstance.OdsInstanceId);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Execute_WithUnknownOdsInstance_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var query = new GetApplicationsByOdsInstanceIdQuery(context);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }
}
