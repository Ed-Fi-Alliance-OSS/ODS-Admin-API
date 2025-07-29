// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class AddApplicationCommandTests : PlatformUsersContextTestBase
{
    private IOptions<AppSettings> _options { get; set; }

    [OneTimeSetUp]
    public virtual async Task FixtureSetup()
    {
        AppSettings appSettings = new AppSettings();
        appSettings.PreventDuplicateApplications = false;
        _options = Options.Create(appSettings);
        await Task.Yield();
    }

    [Test]
    public void ShouldFailForInvalidVendor()
    {
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        Save(vendor);

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = null,
                VendorId = 0
            };

            Assert.Throws<InvalidOperationException>(() => command.Execute(newApplication, _options));
        });
    }

    [Test]
    public void ProfileShouldBeOptional()
    {
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(vendor, odsInstance);

        AddApplicationResult result = null;

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = null,
                VendorId = vendor.VendorId,
                EducationOrganizationIds = new List<long> { 12345, 67890, 5000000005 },
                OdsInstanceIds = new List<int> { odsInstance.OdsInstanceId },
            };

            result = command.Execute(newApplication, _options);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Vendor)
            .Include(a => a.ApiClients)
            .FirstOrDefault(a => a.ApplicationId == result.ApplicationId);

            persistedApplication.ClaimSetName.ShouldBe("FakeClaimSet");
            persistedApplication.Profiles.Count.ShouldBe(0);
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(3);
            persistedApplication.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890 || o.EducationOrganizationId == 5000000005).ShouldBeTrue();

            persistedApplication.Vendor.VendorId.ShouldBeGreaterThan(0);
            persistedApplication.Vendor.VendorId.ShouldBe(vendor.VendorId);

            persistedApplication.ApiClients.Count.ShouldBe(1);
            var apiClient = persistedApplication.ApiClients.FirstOrDefault();
            apiClient.Name.ShouldBe("Test Application");
            apiClient.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890 || o.EducationOrganizationId == 5000000005).ShouldBeTrue();
            apiClient.Key.ShouldBe(result.Key);
            apiClient.Secret.ShouldBe(result.Secret);
        });
    }

    [Test]
    public void ShouldExecute()
    {
        const string OdsInstanceName = "Test Instance";
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        var profile = new Profile
        {
            ProfileName = "Test Profile"
        };

        var odsInstance = new OdsInstance
        {
            Name = OdsInstanceName,
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(vendor, profile, odsInstance);

        AddApplicationResult result = null;
        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = new List<int>() { profile.ProfileId },
                VendorId = vendor.VendorId,
                EducationOrganizationIds = new List<long> { 12345, 67890, 5000000005 },
                OdsInstanceIds = new List<int> { odsInstance.OdsInstanceId },
            };

            result = command.Execute(newApplication, _options);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Vendor)
            .Include(a => a.Profiles)
            .Include(a => a.ApiClients).Single(a => a.ApplicationId == result.ApplicationId);
            persistedApplication.ClaimSetName.ShouldBe("FakeClaimSet");
            persistedApplication.Profiles.Count.ShouldBe(1);
            persistedApplication.Profiles.First().ProfileName.ShouldBe("Test Profile");
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(3);
            persistedApplication.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890 || o.EducationOrganizationId == 5000000005).ShouldBeTrue();

            persistedApplication.Vendor.VendorId.ShouldBeGreaterThan(0);
            persistedApplication.Vendor.VendorId.ShouldBe(vendor.VendorId);

            persistedApplication.ApiClients.Count.ShouldBe(1);
            var apiClient = persistedApplication.ApiClients.First();
            apiClient.Name.ShouldBe("Test Application");
            apiClient.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890 || o.EducationOrganizationId == 5000000005).ShouldBeTrue();
            apiClient.Key.ShouldBe(result.Key);
            apiClient.Secret.ShouldBe(result.Secret);

            var persistedApiOdsInstances = usersContext.ApiClientOdsInstances.Where(a => a.ApiClient.ApiClientId == apiClient.ApiClientId).ToList();

            persistedApiOdsInstances.ShouldNotBeNull();
            persistedApiOdsInstances.First().ApiClient.ApiClientId.ShouldBe(apiClient.ApiClientId);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(p => p.ApiClients)
            .Single(a => a.ApplicationId == result.ApplicationId);
            var apiClient = persistedApplication.ApiClients.First();
            var apiClientOdsInstance = usersContext.ApiClientOdsInstances
            .Include(ac => ac.OdsInstance)
            .Include(ac => ac.ApiClient)
            .FirstOrDefault(o => o.OdsInstance.OdsInstanceId == odsInstance.OdsInstanceId && o.ApiClient.ApiClientId == apiClient.ApiClientId);
            apiClientOdsInstance.ApiClient.ApiClientId.ShouldBe(apiClient.ApiClientId);
            apiClientOdsInstance.OdsInstance.OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);
        });
    }

    [Test]
    public void ShouldFailToAddDuplicatedApplication()
    {
        AppSettings appSettings = new AppSettings();
        appSettings.PreventDuplicateApplications = true;
        IOptions<AppSettings> options = Options.Create(appSettings);
        const string OdsInstanceName = "Test Instance";
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };
        var profile = new Profile
        {
            ProfileName = "Test Profile"
        };

        var odsInstance = new OdsInstance
        {
            Name = OdsInstanceName,
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(vendor, profile, odsInstance);

        AddApplicationResult result = null;
        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = [],
                VendorId = vendor.VendorId,
                EducationOrganizationIds = new List<long> { 12345, 67890, 5000000005 },
                OdsInstanceIds = new List<int> { odsInstance.OdsInstanceId }
            };

            result = command.Execute(newApplication, options);
        });

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = [],
                VendorId = vendor.VendorId,
                EducationOrganizationIds = new List<long> { 12345, 67890, 5000000005 },
                OdsInstanceIds = new List<int> { odsInstance.OdsInstanceId }
            };

            Assert.Throws<AdminApiException>(() => command.Execute(newApplication, options));
        });
    }

    [Test]
    public void ShouldFailToAddDuplicatedApplicationNullFields()
    {
        AppSettings appSettings = new AppSettings();
        appSettings.PreventDuplicateApplications = true;
        IOptions<AppSettings> options = Options.Create(appSettings);
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        Save(vendor);
        AddApplicationResult result = null;
        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = null,
                VendorId = vendor.VendorId
            };

            result = command.Execute(newApplication, options);
        });

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = null,
                VendorId = vendor.VendorId
            };

            Assert.Throws<AdminApiException>(() => command.Execute(newApplication, options));
        });
    }

    [Test]
    public void ShouldExecuteWithDuplicatedNamesDifferentVendor()
    {
        AppSettings appSettings = new AppSettings();
        appSettings.PreventDuplicateApplications = true;
        IOptions<AppSettings> options = Options.Create(appSettings);
        var vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        Save(vendor);

        var secondVendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Second Integration Tests"
        };
        Save(secondVendor);
        AddApplicationResult result = null;
        AddApplicationResult secondResult = null;
        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application-Duplicated-Name",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = null,
                VendorId = vendor.VendorId
            };

            result = command.Execute(newApplication, options);
        });

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext);
            var newSecondApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application-Duplicated-Name",
                ClaimSetName = "FakeClaimSet",
                ProfileIds = null,
                VendorId = secondVendor.VendorId
            };
            secondResult = command.Execute(newSecondApplication, options);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Vendor)
            .Include(a => a.Profiles)
            .Include(a => a.ApiClients).Where(a => a.ApplicationId == result.ApplicationId || a.ApplicationId == secondResult.ApplicationId).ToList();
            persistedApplication.TrueForAll(x => x.ApplicationName == "Production-Test Application-Duplicated-Name").ShouldBeTrue();
            persistedApplication.TrueForAll(x => x.ClaimSetName == "FakeClaimSet").ShouldBeTrue();
            persistedApplication.Count.ShouldBe(2);
        });
    }

    private class TestApplication : IAddApplicationModel
    {
        public string ApplicationName { get; set; }
        public int VendorId { get; set; }
        public string ClaimSetName { get; set; }
        public IEnumerable<int> ProfileIds { get; set; }
        public IEnumerable<long> EducationOrganizationIds { get; set; }
        public IEnumerable<int> OdsInstanceIds { get; set; }
        public bool? Enabled { get; set; }
    }
}
