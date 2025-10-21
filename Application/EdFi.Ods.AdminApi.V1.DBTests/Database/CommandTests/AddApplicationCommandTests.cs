// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.CommandTests;

[TestFixture]
public class AddApplicationCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldFailForInvalidVendor()
    {
        // Fix for CS9035: Ensure the required 'Vendor' property is set in the object initializer.  
        var vendor = new Vendor
        {
            VendorNamespacePrefixes =
           [
               new() {
                   NamespacePrefix = "http://tests.com",
                   Vendor = new Vendor() // Set the required 'Vendor' property  
               }
           ],
            VendorName = "Integration Tests"
        };

        Save(vendor);

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext, new InstanceContext());
            var newApplication = new TestApplication
            {
                ApplicationName = "Production-Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileId = 0,
                VendorId = 0
            };

            Assert.Throws<InvalidOperationException>(() => command.Execute(newApplication));
        });
    }

    [Test]
    public void ProfileShouldBeOptional()
    {
        var vendor = new Vendor
        {
            VendorNamespacePrefixes =
           [
               new VendorNamespacePrefix
               {
                   NamespacePrefix = "http://tests.com",
                   Vendor = new Vendor() // Set the required 'Vendor' property  
               }
           ],
            VendorName = "Integration Tests"
        };

        var odsInstance = new OdsInstance
        {
            Name = "test ods instance",
            InstanceType = "test type",
            Status = "test status",
            IsExtended = true,
            Version = "test version"
        };

        Save(vendor, odsInstance);

        AddApplicationResult result = null;

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext, new InstanceContext());
            var newApplication = new TestApplication
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileId = null,
                OdsInstanceId = odsInstance.OdsInstanceId,
                VendorId = vendor.VendorId,
                EducationOrganizationIds = [12345, 67890]
            };

            result = command.Execute(newApplication);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(x => x.Profiles)
            .Include(x => x.ApplicationEducationOrganizations)
            .Include(x => x.Vendor)
            .Include(x => x.ApiClients)
            .Include(x => x.OdsInstance)
            .Single(a => a.ApplicationId == result.ApplicationId);

            persistedApplication.ClaimSetName.ShouldBe("FakeClaimSet");
            persistedApplication.Profiles.Count.ShouldBe(0);
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(2);
            persistedApplication.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890).ShouldBeTrue();

            persistedApplication.Vendor.VendorId.ShouldBeGreaterThan(0);
            persistedApplication.Vendor.VendorId.ShouldBe(vendor.VendorId);

            persistedApplication.OdsInstance.OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);

            persistedApplication.ApiClients.Count.ShouldBe(1);
            var apiClient = persistedApplication.ApiClients.First();
            apiClient.Name.ShouldBe("Test Application");
            apiClient.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890).ShouldBeTrue();
            apiClient.Key.ShouldBe(result.Key);
            apiClient.Secret.ShouldBe(result.Secret);
        });
    }

    [Test]
    public void OdsInstanceShouldBeOptional()
    {
        var vendor = new Vendor
        {
            VendorNamespacePrefixes =
           [
               new VendorNamespacePrefix
               {
                   NamespacePrefix = "http://tests.com",
                   Vendor = new Vendor() // Fix: Set the required 'Vendor' property  
               }
           ],
            VendorName = "Integration Tests"
        };

        Save(vendor);

        AddApplicationResult result = null;

        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext, new InstanceContext());
            var newApplication = new TestApplication
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileId = null,
                OdsInstanceId = null,
                VendorId = vendor.VendorId,
                EducationOrganizationIds = [12345, 67890]
            };

            result = command.Execute(newApplication);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(x => x.Profiles)
            .Include(x => x.OdsInstance)
            .Include(x => x.ApplicationEducationOrganizations)
            .Include(x => x.Vendor)
            .Include(x => x.ApiClients)
            .Single(a => a.ApplicationId == result.ApplicationId);

            persistedApplication.ClaimSetName.ShouldBe("FakeClaimSet");
            persistedApplication.OdsInstance.ShouldBeNull();
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(2);
            persistedApplication.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890).ShouldBeTrue();

            persistedApplication.Vendor.VendorId.ShouldBeGreaterThan(0);
            persistedApplication.Vendor.VendorId.ShouldBe(vendor.VendorId);

            persistedApplication.ApiClients.Count.ShouldBe(1);
            var apiClient = persistedApplication.ApiClients.First();
            apiClient.Name.ShouldBe("Test Application");
            apiClient.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890).ShouldBeTrue();
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
            VendorNamespacePrefixes =
           [
               new VendorNamespacePrefix
               {
                   NamespacePrefix = "http://tests.com",
                   Vendor = new Vendor { VendorName = "Integration Tests" }
               }
           ],
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
            IsExtended = false,
            Status = "OK",
            Version = "1.0.0"
        };

        Save(vendor, profile, odsInstance);

        var instanceContext = new InstanceContext
        {
            Id = 1,
            Name = OdsInstanceName
        };

        AddApplicationResult result = null;
        Transaction(usersContext =>
        {
            var command = new AddApplicationCommand(usersContext, instanceContext);
            var newApplication = new TestApplication
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                ProfileId = profile.ProfileId,
                OdsInstanceId = odsInstance.OdsInstanceId,
                VendorId = vendor.VendorId,
                EducationOrganizationIds = [12345, 67890]
            };

            result = command.Execute(newApplication);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(x => x.Profiles)
            .Include(x => x.ApplicationEducationOrganizations)
            .Include(x => x.Vendor)
            .Include(x => x.ApiClients)
            .Include(x => x.OdsInstance)
            .Single(a => a.ApplicationId == result.ApplicationId);

            persistedApplication.ClaimSetName.ShouldBe("FakeClaimSet");
            persistedApplication.Profiles.Count.ShouldBe(1);
            persistedApplication.Profiles.First().ProfileName.ShouldBe("Test Profile");
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(2);
            persistedApplication.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890).ShouldBeTrue();

            persistedApplication.Vendor.VendorId.ShouldBeGreaterThan(0);
            persistedApplication.Vendor.VendorId.ShouldBe(vendor.VendorId);

            persistedApplication.ApiClients.Count.ShouldBe(1);
            var apiClient = persistedApplication.ApiClients.First();
            apiClient.Name.ShouldBe("Test Application");
            apiClient.ApplicationEducationOrganizations.All(o => o.EducationOrganizationId == 12345 || o.EducationOrganizationId == 67890).ShouldBeTrue();
            apiClient.Key.ShouldBe(result.Key);
            apiClient.Secret.ShouldBe(result.Secret);

            persistedApplication.OdsInstance.ShouldNotBeNull();
            persistedApplication.OdsInstance.Name.ShouldBe(OdsInstanceName);
        });
    }

    private class TestApplication : IAddApplicationModel
    {
        public string ApplicationName { get; set; }
        public int VendorId { get; set; }
        public string ClaimSetName { get; set; }
        public int? ProfileId { get; set; }
        public int? OdsInstanceId { get; set; }
        public IEnumerable<int> EducationOrganizationIds { get; set; }
    }
}
