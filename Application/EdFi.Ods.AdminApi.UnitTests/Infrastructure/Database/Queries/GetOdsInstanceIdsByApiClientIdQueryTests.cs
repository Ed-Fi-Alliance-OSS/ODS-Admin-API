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
public class GetOdsInstanceIdsByApiClientIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceIdsByApiClientIdQueryTests_{Guid.NewGuid()}")
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
    public void Execute_WithApiClientId_ReturnsMappedOdsInstanceIds()
    {
        using var context = CreateContext();
        var application = CreateApplication(new Vendor { VendorName = "Acme Vendor" });
        var apiClient = new ApiClient(true)
        {
            Name = "Client One",
            Application = application,
            User = new User { FullName = "Client User", Email = "client@test.org" }
        };
        var odsOne = new OdsInstance { Name = "Sandbox A", InstanceType = "type", ConnectionString = "cs1" };
        var odsTwo = new OdsInstance { Name = "Sandbox B", InstanceType = "type", ConnectionString = "cs2" };
        application.ApiClients.Add(apiClient);
        context.ApiClientOdsInstances.AddRange(
            new ApiClientOdsInstance { ApiClient = apiClient, OdsInstance = odsOne },
            new ApiClientOdsInstance { ApiClient = apiClient, OdsInstance = odsTwo });
        context.SaveChanges();

        var query = new GetOdsInstanceIdsByApiClientIdQuery(context);

        var result = query.Execute(apiClient.ApiClientId);

        result.ShouldBe([odsOne.OdsInstanceId, odsTwo.OdsInstanceId], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithUnknownApiClientId_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var query = new GetOdsInstanceIdsByApiClientIdQuery(context);

        var result = query.Execute(999);

        result.ShouldBeEmpty();
    }

    [Test]
    public void Execute_WithApiClientIds_ReturnsGroupedMappings()
    {
        using var context = CreateContext();
        var application = CreateApplication(new Vendor { VendorName = "Acme Vendor" });
        var firstClient = new ApiClient(true)
        {
            Name = "Client One",
            Application = application,
            User = new User { FullName = "Client One User", Email = "clientone@test.org" }
        };
        var secondClient = new ApiClient(true)
        {
            Name = "Client Two",
            Application = application,
            User = new User { FullName = "Client Two User", Email = "clienttwo@test.org" }
        };
        application.ApiClients.Add(firstClient);
        application.ApiClients.Add(secondClient);
        var odsOne = new OdsInstance { Name = "Sandbox A", InstanceType = "type", ConnectionString = "cs1" };
        var odsTwo = new OdsInstance { Name = "Sandbox B", InstanceType = "type", ConnectionString = "cs2" };
        context.ApiClientOdsInstances.AddRange(
            new ApiClientOdsInstance { ApiClient = firstClient, OdsInstance = odsOne },
            new ApiClientOdsInstance { ApiClient = secondClient, OdsInstance = odsTwo });
        context.SaveChanges();

        var query = new GetOdsInstanceIdsByApiClientIdQuery(context);

        var result = query.Execute(new[] { firstClient.ApiClientId, secondClient.ApiClientId });

        result.Count.ShouldBe(2);
        result[firstClient.ApiClientId].Single().ShouldBe(odsOne.OdsInstanceId);
        result[secondClient.ApiClientId].Single().ShouldBe(odsTwo.OdsInstanceId);
    }
}
