// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
internal class AddApiClientCommandTests : PlatformUsersContextTestBase
{
    private IOptions<AppSettings> _options { get; set; }
    private int applicationId { get; set; }

    [SetUp]
    public new virtual async Task SetUp()
    {
        AppSettings appSettings = new()
        {
            PreventDuplicateApplications = false
        };
        _options = Options.Create(appSettings);
        await Task.Yield();

        var vendor = new Vendor
        {
            VendorId = 0,
            VendorNamespacePrefixes = [new() { NamespacePrefix = "http://tests.com" }],
            VendorName = "Integration Tests"
        };

        var application = new Application
        {
            ApplicationName = "Test Application",
            ClaimSetName = "FakeClaimSet",
            OperationalContextUri = "http://test.com",
            Profiles = null,
            Vendor = vendor
        };

        Save(application);

        applicationId = application.ApplicationId;
    }

    [Test]
    public void ShouldFailForInvalidApplication()
    {
        Transaction(usersContext =>
        {
            var command = new AddApiClientCommand(usersContext);
            var newApiClient = new TestApiClient
            {
                Name = "Test ApiClient",
                ApplicationId = 0,
                IsApproved = true,
                OdsInstanceIds = [1, 2]
            };

            Assert.Throws<InvalidOperationException>(() => command.Execute(newApiClient, _options));
        });
    }

    [Test]
    public void ShouldCreateApiClientWithOdsInstances()
    {
        var vendor = new Vendor
        {
            VendorId = 0,
            VendorNamespacePrefixes = [new() { NamespacePrefix = "http://tests.com" }],
            VendorName = "Integration Tests"
        };

        var application = new Application
        {
            ApplicationName = "Test Application",
            ClaimSetName = "FakeClaimSet",
            OperationalContextUri = "http://test.com",
            Profiles = null,
            Vendor = vendor
        };

        Save(application);

        Transaction(usersContext =>
        {
            var command = new AddApiClientCommand(usersContext);
            var newApiClient = new TestApiClient
            {
                Name = "Test ApiClient",
                ApplicationId = application.ApplicationId,
                IsApproved = true,
                OdsInstanceIds = [1, 2]
            };

            command.Execute(newApiClient, _options);
        });
    }

    [Test]
    public void ShouldCreateApiClientWithOutOdsInstances()
    {
        var vendor = new Vendor
        {
            VendorId = 0,
            VendorNamespacePrefixes = [new() { NamespacePrefix = "http://tests.com" }],
            VendorName = "Integration Tests"
        };

        var application = new Application
        {
            ApplicationName = "Test Application",
            ClaimSetName = "FakeClaimSet",
            OperationalContextUri = "http://test.com",
            Profiles = null,
            Vendor = vendor
        };

        Save(application);

        Transaction(usersContext =>
        {
            var command = new AddApiClientCommand(usersContext);
            var newApiClient = new TestApiClient
            {
                Name = "Test ApiClient",
                ApplicationId = application.ApplicationId,
                IsApproved = true,
                OdsInstanceIds = null // No OdsInstanceIds provided
            };

            command.Execute(newApiClient, _options);
        });
    }

    private class TestApiClient : IAddApiClientModel
    {
        public string Name { get; set; }
        public bool IsApproved { get; set; }
        public int ApplicationId { get; set; }
        public IEnumerable<int> OdsInstanceIds { get; set; }
    }
}
