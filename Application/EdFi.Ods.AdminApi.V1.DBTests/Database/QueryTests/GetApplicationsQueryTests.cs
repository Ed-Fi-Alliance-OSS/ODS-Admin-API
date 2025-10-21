// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.QueryTests;

[TestFixture]
public class GetApplicationsQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_retrieve_vendors()
    {
        var newVendor = new Vendor
        {
            VendorName = "test vendor",
            VendorNamespacePrefixes = [new VendorNamespacePrefix { NamespacePrefix = "http://testvendor.net", Vendor = new Vendor { VendorName = "Integration Tests" } }],
        };

        Save(newVendor);

        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());
            var allVendors = command.Execute();

            allVendors.ShouldNotBeEmpty();

            var vendor = allVendors.Single(v => v.VendorId == newVendor.VendorId);
            vendor.VendorName.ShouldBe("test vendor");
            vendor.VendorNamespacePrefixes.First().NamespacePrefix.ShouldBe("http://testvendor.net");
        });
    }

    [Test]
    public void Should_retrieve_applications_with_offset_and_limit()
    {
        var vendors = new Vendor[5];

        for (var vendorIndex = 0; vendorIndex < 5; vendorIndex++)
        {
            vendors[vendorIndex] = new Vendor
            {
                VendorName = $"test vendor {vendorIndex + 1}",
                VendorNamespacePrefixes = [new VendorNamespacePrefix { NamespacePrefix = "http://testvendor.net", Vendor = new Vendor { VendorName = "Integration Tests" } }],
                Applications =
                [
                    new Application
                    {
                        ApplicationName = $"test app {vendorIndex + 1}",
                        ClaimSetName = "Ed-Fi API Publisher - Reader",
                        OperationalContextUri = $"test app {vendorIndex + 1}",
                        ApplicationEducationOrganizations =
                        [
                            new ApplicationEducationOrganization
                            {
                                EducationOrganizationId = 0,
                            }
                        ]
                    }
                ]
            };
        }

        Save(vendors);

        Transaction(usersContext =>
        {
            var command = new GetApplicationsQuery(usersContext, Testing.GetAppSettings());
            var commonQueryParams = new CommonQueryParams(0, 2);

            var applicationsAfterOffset = command.Execute(commonQueryParams);

            applicationsAfterOffset.ShouldNotBeEmpty();
            applicationsAfterOffset.Count.ShouldBe(2);

            applicationsAfterOffset.ShouldContain(v => v.ApplicationName == "test app 1");
            applicationsAfterOffset.ShouldContain(v => v.ApplicationName == "test app 2");

            commonQueryParams.Offset = 2;

            applicationsAfterOffset = command.Execute(commonQueryParams);

            applicationsAfterOffset.ShouldNotBeEmpty();
            applicationsAfterOffset.Count.ShouldBe(2);

            applicationsAfterOffset.ShouldContain(v => v.ApplicationName == "test app 3");
            applicationsAfterOffset.ShouldContain(v => v.ApplicationName == "test app 4");
            commonQueryParams.Offset = 4;

            applicationsAfterOffset = command.Execute(commonQueryParams);

            applicationsAfterOffset.ShouldNotBeEmpty();
            applicationsAfterOffset.Count.ShouldBe(1);

            applicationsAfterOffset.ShouldContain(v => v.ApplicationName == "test app 5");
        });
    }
}
