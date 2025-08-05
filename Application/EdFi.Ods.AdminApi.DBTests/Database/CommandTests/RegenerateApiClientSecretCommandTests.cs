// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using NUnit.Framework;
using Shouldly;
using VendorUser = EdFi.Admin.DataAccess.Models.User;
using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class RegenerateApiClientSecretCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldFailIfApiClientDoesNotExist()
    {
        Transaction(usersContext =>
        {
            var command = new RegenerateApiClientSecretCommand(usersContext);
            Assert.Throws<NotFoundException<int>>(() => command.Execute(0));
        });
    }

    [Test]
    public void ShouldUpdateApiClientSecret()
    {
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        var user = new VendorUser
        {
            Email = "nobody@nowhere.com",
            FullName = "Integration Tests",
            Vendor = vendor
        };

        var profile = new Profile
        {
            ProfileName = "Test Profile"
        };

        var apiClient = new ApiClient(true)
        {
            Name = "Integration Test"
        };

        var application = new Application
        {
            ApplicationName = "Test Application",
            ClaimSetName = "FakeClaimSet",
            ApiClients = new List<ApiClient>(),
            Vendor = vendor,
            Profiles = new List<Profile>(),
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };

        application.ApiClients.Add(apiClient);
        application.Profiles.Add(profile);

        Save(vendor, user, profile, application);

        var orignalKey = apiClient.Key;
        var originalSecret = apiClient.Secret;

        //Simulate the automatic hashing performed by using the key/secret on the API.
        Transaction(usersContext =>
        {
            var odsSideApiClient = usersContext.ApiClients.Single(c => c.ApiClientId == apiClient.ApiClientId);
            odsSideApiClient.Secret = "SIMULATED HASH OF " + originalSecret;
            odsSideApiClient.SecretIsHashed = true;
        });

        RegenerateApiClientSecretResult result = null;
        Transaction(usersContext =>
        {
            var command = new RegenerateApiClientSecretCommand(usersContext);
            result = command.Execute(apiClient.ApiClientId);
        });

        var updatedApiClient = Transaction(usersContext => usersContext.ApiClients.Single(c => c.ApiClientId == apiClient.ApiClientId));

        result.Key.ShouldBe(orignalKey);
        result.Name.ShouldBe("Integration Test");
        result.Secret.ShouldNotBe(originalSecret);
        result.Secret.ShouldNotBe("SIMULATED HASH OF " + originalSecret);
        result.Secret.ShouldNotBeEmpty();

        updatedApiClient.Key.ShouldBe(result.Key);
        updatedApiClient.Secret.ShouldBe(result.Secret);
        updatedApiClient.SecretIsHashed.ShouldBe(false);
    }
}
