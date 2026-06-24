// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.Vendors;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors;

[TestFixture]
public class VendorMapperTests
{
    [Test]
    public void ToModel_MapsAllFieldsCorrectly()
    {
        var vendor = new Vendor
        {
            VendorId = 1,
            VendorName = "Acme Vendor",
            VendorNamespacePrefixes = new List<VendorNamespacePrefix>
            {
                new VendorNamespacePrefix { NamespacePrefix = "http://acme.org/ns" }
            },
            Users = new List<User>
            {
                new User { FullName = "Alice", Email = "alice@acme.org" }
            }
        };

        var model = VendorMapper.ToModel(vendor);

        model.Id.ShouldBe(1);
        model.Company.ShouldBe("Acme Vendor");
        model.NamespacePrefixes.ShouldBe("http://acme.org/ns");
        model.ContactName.ShouldBe("Alice");
        model.ContactEmailAddress.ShouldBe("alice@acme.org");
    }

    [Test]
    public void ToModel_WithNoNamespacePrefixes_ReturnsEmptyString()
    {
        var vendor = new Vendor
        {
            VendorId = 1,
            VendorName = "Acme Vendor",
            VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
            Users = new List<User>
            {
                new User { FullName = "Alice", Email = "alice@acme.org" }
            }
        };

        var model = VendorMapper.ToModel(vendor);

        model.NamespacePrefixes.ShouldBe(string.Empty);
    }

    [Test]
    public void ToModel_WithNoUsers_MapsContactFieldsAsNull()
    {
        var vendor = new Vendor
        {
            VendorId = 1,
            VendorName = "Acme Vendor",
            VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
            Users = new List<User>()
        };

        var model = VendorMapper.ToModel(vendor);

        model.ContactName.ShouldBeNull();
        model.ContactEmailAddress.ShouldBeNull();
    }

    [Test]
    public void ToModelList_MapsMultipleVendors()
    {
        var vendors = new List<Vendor>
        {
            new Vendor
            {
                VendorId = 1,
                VendorName = "Acme Vendor",
                VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
                Users = new List<User>()
            },
            new Vendor
            {
                VendorId = 2,
                VendorName = "Beta Vendor",
                VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
                Users = new List<User>()
            }
        };

        var models = VendorMapper.ToModelList(vendors);

        models.Count.ShouldBe(2);
        models[0].Company.ShouldBe("Acme Vendor");
        models[1].Company.ShouldBe("Beta Vendor");
    }
}
