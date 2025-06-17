// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Profile = EdFi.Admin.DataAccess.Models.Profile;
using VendorUser = EdFi.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class EditApplicationCommandTests : PlatformUsersContextTestBase
{
    private Vendor _vendor;
    private Vendor _otherVendor;
    private VendorUser _user;
    private VendorUser _otherUser;
    private Profile _profile;
    private Profile _otherProfile;
    private ApiClient _apiClient;
    private Application _application;
    private OdsInstance _odsInstance;


    private void SetupTestEntities()
    {
        _odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        _vendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Integration Tests"
        };

        _otherVendor = new Vendor
        {
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
            VendorName = "Different Integration Tests"
        };

        _user = new VendorUser
        {
            Email = "nobody@nowhere.com",
            FullName = "Integration Tests",
            Vendor = _vendor
        };

        _otherUser = new VendorUser
        {
            Email = "nowhere@nobody.com",
            FullName = "Different Integration Tests",
            Vendor = _otherVendor
        };

        _profile = new Profile
        {
            ProfileName = "Test Profile"
        };

        _otherProfile = new Profile
        {
            ProfileName = "Other Test Profile"
        };

        _apiClient = new ApiClient(true)
        {
            Name = "Integration Test",
            UseSandbox = false,
        };

        _application = new Application
        {
            ApplicationName = "Test Application",
            ClaimSetName = "FakeClaimSet",
            Vendor = _vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };

        _application.ApiClients.Add(_apiClient);
        _application.Profiles.Add(_profile);
        _application.ApplicationEducationOrganizations.Add(_application.CreateApplicationEducationOrganization(12345));
        _application.ApplicationEducationOrganizations.Add(_application.CreateApplicationEducationOrganization(67890));

        Save(_vendor, _otherVendor, _user, _otherUser, _profile, _otherProfile, _application, _odsInstance);
    }

    [Test]
    public void ShouldRemoveProfileIfNull()
    {
        SetupTestEntities();

        var editModel = new TestEditApplicationModel
        {
            Id = _application.ApplicationId,
            ApplicationName = _application.ApplicationName,
            ClaimSetName = _application.ClaimSetName,
            EducationOrganizationIds = new List<long> { 12345, 67890 },
            ProfileIds = null,
            VendorId = _vendor.VendorId,
            OdsInstanceIds = new List<int> { _odsInstance.OdsInstanceId }
        };

        Transaction(usersContext =>
        {
            var command = new EditApplicationCommand(usersContext);
            command.Execute(editModel);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(a => a.ApiClients)
            .Include(a => a.Profiles)
            .Include(a => a.ApplicationEducationOrganizations)
            .Single(a => a.ApplicationId == _application.ApplicationId);
            persistedApplication.ApplicationName.ShouldBe("Test Application");
            persistedApplication.ClaimSetName.ShouldBe("FakeClaimSet");
            persistedApplication.ApiClients.Count.ShouldBe(1);
            persistedApplication.ApiClients.First().Name.ShouldBe("Test Application");
            persistedApplication.ApiClients.First().ApplicationEducationOrganizations.ShouldAllBe(aeo => persistedApplication.ApplicationEducationOrganizations.Contains(aeo));
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(2);
            persistedApplication.ApplicationEducationOrganizations.ShouldAllBe(aeo => aeo.EducationOrganizationId == 12345 || aeo.EducationOrganizationId == 67890);
            persistedApplication.Profiles.Count.ShouldBe(0);
        });
    }

    [Test]
    public void ShouldUpdateAllEntitiesProperly()
    {
        SetupTestEntities();

        var editModel = new TestEditApplicationModel
        {
            Id = _application.ApplicationId,
            ApplicationName = "New Application Name",
            ClaimSetName = "DifferentFakeClaimSet",
            EducationOrganizationIds = new List<long> { 23456, 78901, 5000000005 },
            ProfileIds = new List<int>() { _otherProfile.ProfileId },
            VendorId = _otherVendor.VendorId,
            OdsInstanceIds = new List<int> { _odsInstance.OdsInstanceId }
        };

        Transaction(usersContext =>
        {
            var command = new EditApplicationCommand(usersContext);
            command.Execute(editModel);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(a => a.ApiClients)
            .Include(a => a.Profiles)
            .Include(a => a.ApplicationEducationOrganizations).Single(a => a.ApplicationId == _application.ApplicationId);
            persistedApplication.ApplicationName.ShouldBe("New Application Name");
            persistedApplication.ClaimSetName.ShouldBe("DifferentFakeClaimSet");
            persistedApplication.ApiClients.Count.ShouldBe(1);
            persistedApplication.ApiClients.First().Name.ShouldBe("New Application Name");
            persistedApplication.ApiClients.First().ApplicationEducationOrganizations.ShouldAllBe(aeo => persistedApplication.ApplicationEducationOrganizations.Contains(aeo));
            persistedApplication.Profiles.Count.ShouldBe(1);
            persistedApplication.Profiles.First().ProfileName.ShouldBe("Other Test Profile");
            persistedApplication.ApplicationEducationOrganizations.Count.ShouldBe(3);
            persistedApplication.ApplicationEducationOrganizations.ShouldAllBe(aeo => aeo.EducationOrganizationId == 23456 || aeo.EducationOrganizationId == 78901 || aeo.EducationOrganizationId == 5000000005);
        });

        Transaction(usersContext =>
        {
            var persistedApplication = usersContext.Applications
            .Include(a => a.ApiClients)
            .Include(a => a.Profiles)
            .Include(a => a.ApplicationEducationOrganizations).Single(a => a.ApplicationId == _application.ApplicationId); 
            var apiClient = persistedApplication.ApiClients.First();
            var odsInstanceId = _odsInstance.OdsInstanceId;
            var apiClientOdsInstance = usersContext.ApiClientOdsInstances
            .Include(a => a.OdsInstance)
            .Include(a => a.ApiClient)
            .FirstOrDefault(o => o.OdsInstance.OdsInstanceId == odsInstanceId && o.ApiClient.ApiClientId == apiClient.ApiClientId);
            apiClientOdsInstance.ApiClient.ApiClientId.ShouldBe(apiClient.ApiClientId);
            apiClientOdsInstance.OdsInstance.OdsInstanceId.ShouldBe(_odsInstance.OdsInstanceId);
        });


    }

    private class TestEditApplicationModel : IEditApplicationModel
    {
        public int Id { get; set; }
        public string ApplicationName { get; set; }
        public int VendorId { get; set; }
        public string ClaimSetName { get; set; }
        public IEnumerable<int> ProfileIds { get; set; }
        public IEnumerable<long> EducationOrganizationIds { get; set; }
        public IEnumerable<int> OdsInstanceIds { get; set; }
        public bool? Enabled { get; set; }
    }
}
