// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetVendorsQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_retrieve_vendors()
    {
        var newVendor = new Vendor
        {
            VendorName = "test vendor",
            VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://testvendor.net" } },
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
    public void Should_retrieve_vendors_with_no_namespaces()
    {
        var newVendor = new Vendor
        {
            VendorName = "test vendor without namespace",
            VendorNamespacePrefixes = null,
        };

        Save(newVendor);

        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());
            var allVendors = command.Execute();

            allVendors.ShouldNotBeEmpty();

            var vendor = allVendors.Single(v => v.VendorId == newVendor.VendorId);
            vendor.VendorName.ShouldBe("test vendor without namespace");
            vendor.VendorNamespacePrefixes.ShouldBeEmpty();
        });
    }

    [Test]
    public void Should_retrieve_vendors_with_filters()
    {
        var vendors = new Vendor[5];

        var offset = 0;
        var limit = 2;

        for (var vendorIndex = 0; vendorIndex < 5; vendorIndex++)
        {
            vendors[vendorIndex] = new Vendor
            {
                VendorName = $"test vendor {vendorIndex + 1}",
                VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = $"http://testvendor{vendorIndex + 1}.net" } },
                Users = new List<User> { new User { FullName = $"test user {vendorIndex + 1}", Email = $"testuser{vendorIndex + 1}@test.com" } }
            };
        }

        Save(vendors);

        /// Id
        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());

            var vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), vendors.First().VendorId, null, null, null, null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(1);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 1");
        });

        /// Company
        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());

            var vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, "test vendor 2", null, null, null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(1);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 2");
        });

        /// NamespacePrefix
        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());

            var vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, "http://testvendor2.net", null, null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(1);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 2");
        });

        /// ContactName
        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());

            var vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null, "test user 2", null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(1);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 2");
        });

        /// ContactEmailAddress
        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());

            var vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null, null, "testuser2@test.com");

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(1);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 2");
        });
    }

    [Test]
    public void Should_retrieve_vendors_with_offset_and_limit()
    {
        var vendors = new Vendor[5];

        for (var vendorIndex = 0; vendorIndex < 5; vendorIndex++)
        {
            vendors[vendorIndex] = new Vendor
            {
                VendorName = $"test vendor {vendorIndex + 1}",
                VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://testvendor.net" } },
                Users = new List<User> { new User { FullName = $"test user", Email = $"testuser@test.com" } }
            };
        }

        Save(vendors);

        Transaction(usersContext =>
        {
            var command = new GetVendorsQuery(usersContext, Testing.GetAppSettings());

            var offset = 0;
            var limit = 2;

            var vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null, null, null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(2);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 1");
            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 2");

            offset = 2;

            vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null, null, null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(2);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 3");
            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 4");
            offset = 4;

            vendorsAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null, null, null);

            vendorsAfterOffset.ShouldNotBeEmpty();
            vendorsAfterOffset.Count.ShouldBe(1);

            vendorsAfterOffset.ShouldContain(v => v.VendorName == "test vendor 5");
        });
    }
}
