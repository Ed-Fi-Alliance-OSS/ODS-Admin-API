// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.DBTestsShared;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
internal class AddApiClientCommandTests : PlatformUsersContextTestBase
{
    protected override string AdminConnectionString => Testing.AdminConnectionString;

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

            // ADMINAPI-1514: AddApiClientCommand now throws the typed NotFoundException that
            // EditApplicationCommand already used, instead of a bare InvalidOperationException.
            Assert.Throws<NotFoundException<int>>(() => command.Execute(newApiClient, _options));
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
    public void ShouldCreateApiClientWithoutOdsInstances()
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

    [Test]
    public void ShouldCreateApiClientWithIsApprovedFalse()
    {
        Transaction(usersContext =>
        {
            var command = new AddApiClientCommand(usersContext);
            var newApiClient = new TestApiClient
            {
                Name = "Test ApiClient IsApproved False",
                ApplicationId = applicationId,
                IsApproved = false,
                OdsInstanceIds = null
            };

            var result = command.Execute(newApiClient, _options);
            var persistedApiClient = usersContext.ApiClients.Find(result.Id);

            persistedApiClient.ShouldNotBeNull();
            persistedApiClient.IsApproved.ShouldBeFalse();
        });
    }


    [Test]
    public void ShouldGiveNewApiClientTheApplicationsEducationOrganizations()
    {
        var vendor = new Vendor
        {
            VendorId = 0,
            VendorNamespacePrefixes = [new() { NamespacePrefix = "http://tests.com" }],
            VendorName = "Integration Tests EdOrg Reuse"
        };

        var application = new Application
        {
            ApplicationName = "EdOrg Reuse Application",
            ClaimSetName = "FakeClaimSet",
            OperationalContextUri = "http://test.com",
            Profiles = null,
            Vendor = vendor
        };
        application.ApplicationEducationOrganizations.Add(application.CreateApplicationEducationOrganization(12345));
        application.ApplicationEducationOrganizations.Add(application.CreateApplicationEducationOrganization(67890));

        Save(application);

        var newApiClientId = 0;
        Transaction(usersContext =>
        {
            var command = new AddApiClientCommand(usersContext);
            var result = command.Execute(new TestApiClient
            {
                Name = "EdOrg Reuse Credential",
                ApplicationId = application.ApplicationId,
                IsApproved = true,
                OdsInstanceIds = null
            }, _options);
            newApiClientId = result.Id;
        });

        Transaction(usersContext =>
        {
            // ADMINAPI-1514: the new credential joins the Application's existing rows.
            // The row count must not grow - parallel copies would be a duplication bug.
            var applicationEdOrgIds = usersContext.ApplicationEducationOrganizations
                .Where(a => a.Application.ApplicationId == application.ApplicationId)
                .Select(a => a.ApplicationEducationOrganizationId)
                .OrderBy(id => id)
                .ToList();

            applicationEdOrgIds.Count.ShouldBe(2);

            var createdApiClient = usersContext.ApiClients
                .Include(c => c.ApplicationEducationOrganizations)
                .Single(c => c.ApiClientId == newApiClientId);

            createdApiClient.ApplicationEducationOrganizations
                .Select(a => a.ApplicationEducationOrganizationId)
                .OrderBy(id => id)
                .ToList()
                .ShouldBe(applicationEdOrgIds);
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
