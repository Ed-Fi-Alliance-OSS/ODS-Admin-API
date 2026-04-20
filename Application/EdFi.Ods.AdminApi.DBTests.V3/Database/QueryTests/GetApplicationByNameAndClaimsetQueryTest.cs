// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.QueryTests;

[TestFixture]
public class GetApplicationByNameAndClaimsetQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetBasicApplicationData()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var applicationName = "test application";
        var claimsetName = "test claim set";
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };
        vendor.Applications.Add(application);
        Save(vendor);

        Transaction(usersContext =>
        {
            var getApplicationByNameAndClaimsetQuery = new GetApplicationByNameAndClaimsetQuery(usersContext);
            var results = getApplicationByNameAndClaimsetQuery.Execute(applicationName, claimsetName);
            results.ApplicationName.ShouldBe("test application");
            results.ClaimSetName.ShouldBe("test claim set");
        });
    }

    [Test]
    public void ShouldNotGetApplicationDataWithDifferentClaimset()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var applicationName = "test application";
        var claimsetName = "test different claim set";
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };
        vendor.Applications.Add(application);
        Save(vendor);

        Transaction(usersContext =>
        {
            var getApplicationByNameAndClaimsetQuery = new GetApplicationByNameAndClaimsetQuery(usersContext);
            var results = getApplicationByNameAndClaimsetQuery.Execute(applicationName, claimsetName);
            results.ShouldBeNull();
        });
    }

    [Test]
    public void ShouldNotGetApplicationDataWithDifferentApplicationName()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var applicationName = "test different application";
        var claimsetName = "test claim set";
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };
        vendor.Applications.Add(application);
        Save(vendor);

        Transaction(usersContext =>
        {
            var getApplicationByNameAndClaimsetQuery = new GetApplicationByNameAndClaimsetQuery(usersContext);
            var results = getApplicationByNameAndClaimsetQuery.Execute(applicationName, claimsetName);
            results.ShouldBeNull();
        });
    }

    [Test]
    public void ShouldNotGetApplicationDataWithDifferentApplicationNameAndClaimsetName()
    {
        var vendor = new Vendor { VendorName = "test vendor" };
        var applicationName = "test different application";
        var claimsetName = "test different claim set";
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };
        vendor.Applications.Add(application);
        Save(vendor);

        Transaction(usersContext =>
        {
            var getApplicationByNameAndClaimsetQuery = new GetApplicationByNameAndClaimsetQuery(usersContext);
            var results = getApplicationByNameAndClaimsetQuery.Execute(applicationName, claimsetName);
            results.ShouldBeNull();
        });
    }
}



