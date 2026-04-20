// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
using VendorUser = EdFi.Admin.DataAccess.Models.User;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.QueryTests;

[TestFixture]
public class GetApplicationByOdsInstanceIdQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetBasicApplicationData()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };
        var user = new VendorUser
        {
            Email = "",
            FullName = application.ApplicationName,
            Vendor = vendor
        };

        var apiClient = new ApiClient
        {
            Application = application,
            Key = "key",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active",
            User = user,
        };

        var apiClientOdsIntance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };

        Save(odsInstance, vendor, application, apiClient, apiClientOdsIntance);

        Transaction(usersContext =>
        {
            var getApplicationsByOdsInstanceIdQuery = new GetApplicationsByOdsInstanceIdQuery(usersContext);
            var results = getApplicationsByOdsInstanceIdQuery.Execute(odsInstance.OdsInstanceId);
            results.Single().ApplicationName.ShouldBe("test application");
            results.Single().ClaimSetName.ShouldBe("test claim set");
        });
    }

    [Test]
    public void ShouldGetApplicationEducationOrganization()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        var application = new Application
        {
            ApplicationName = "test application",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };

        var applicationOrganization = new ApplicationEducationOrganization { Application = application };

        application.ApplicationEducationOrganizations.Add(applicationOrganization);

        var user = new VendorUser
        {
            Email = "",
            FullName = application.ApplicationName,
            Vendor = vendor
        };

        var apiClient = new ApiClient
        {
            Application = application,
            Key = "key",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active",
            User = user,
        };

        application.ApiClients.Add(apiClient);

        var apiClientOdsIntance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };
        Save(odsInstance, vendor, application, apiClientOdsIntance);

        var organizationId = applicationOrganization.ApplicationEducationOrganizationId;
        organizationId.ShouldBeGreaterThan(0);

        Transaction(usersContext =>
        {
            var getApplicationsByOdsInstanceIdQuery = new GetApplicationsByOdsInstanceIdQuery(usersContext);
            var results = getApplicationsByOdsInstanceIdQuery.Execute(odsInstance.OdsInstanceId);
            results.Single().ApplicationEducationOrganizations.Single().ApplicationEducationOrganizationId.ShouldBe(organizationId);
        });
    }

    [Test]
    public void ShouldGetApplicationProfile()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        var application = new Application
        {
            ApplicationName = "test application",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };
        var profile = new Profile
        {
            Applications = new List<Application> { application },
            ProfileName = "test profile"
        };

        var user = new VendorUser
        {
            Email = "",
            FullName = application.ApplicationName,
            Vendor = vendor
        };

        var apiClient = new ApiClient
        {
            Application = application,
            Key = "key",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active",
            User = user,
        };

        var apiClientOdsIntance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };
        application.Profiles.Add(profile);

        Save(odsInstance, vendor, application, apiClient, apiClientOdsIntance);
        var profileId = profile.ProfileId;
        profileId.ShouldBeGreaterThan(0);

        Transaction(usersContext =>
        {
            var getApplicationsByOdsInstanceIdQuery = new GetApplicationsByOdsInstanceIdQuery(usersContext);
            var results = getApplicationsByOdsInstanceIdQuery.Execute(odsInstance.OdsInstanceId);
            results.Single().Profiles.Single().ProfileId.ShouldBe(profileId);
        });
    }

    [Test]
    public void ShouldThrowWhenOdsInstanceIdIsInvalid()
    {
        Transaction(usersContext =>
        {
            var getApplicationsByOdsInstanceIdQuery = new GetApplicationsByOdsInstanceIdQuery(usersContext);
            Should.Throw<NotFoundException<int>>(() =>
            {
                getApplicationsByOdsInstanceIdQuery.Execute(int.MaxValue);
            });
        });
    }
}



