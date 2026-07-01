// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.Vendors;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors
{
    [TestFixture]
    public class VendorMapperTests
    {
        [Test]
        public void ToModel_MapsAllFields_Correctly()
        {
            var vendor = new Vendor
            {
                VendorId = 1,
                VendorName = "Acme Corp",
                VendorNamespacePrefixes = new List<VendorNamespacePrefix>
                {
                    new VendorNamespacePrefix { NamespacePrefix = "https://acme.org/ns" }
                },
                Users = new List<User>
                {
                    new User { FullName = "Alice", Email = "alice@acme.org" }
                }
            };

            var result = VendorMapper.ToModel(vendor);

            result.Id.ShouldBe(vendor.VendorId);
            result.Company.ShouldBe("Acme Corp");
            result.ContactName.ShouldBe("Alice");
            result.ContactEmailAddress.ShouldBe("alice@acme.org");
            result.NamespacePrefixes.ShouldBe("https://acme.org/ns");
        }

        [Test]
        public void ToModel_WithNoUsers_ReturnsNullContactFields()
        {
            var vendor = new Vendor
            {
                VendorId = 2,
                VendorName = "No Users Corp",
                VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
                Users = new List<User>()
            };

            VendorModel result = null!;
            Should.NotThrow(() => result = VendorMapper.ToModel(vendor));

            result.ContactName.ShouldBeNull();
            result.ContactEmailAddress.ShouldBeNull();
        }

        [Test]
        public void ToModelList_MapsAllVendors_InOrder()
        {
            var vendors = new List<Vendor>
            {
                new Vendor
                {
                    VendorId = 10,
                    VendorName = "First Vendor",
                    VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
                    Users = new List<User>()
                },
                new Vendor
                {
                    VendorId = 20,
                    VendorName = "Second Vendor",
                    VendorNamespacePrefixes = new List<VendorNamespacePrefix>(),
                    Users = new List<User>()
                }
            };

            var results = VendorMapper.ToModelList(vendors);

            results.Count.ShouldBe(2);
            results[0].Company.ShouldBe("First Vendor");
            results[1].Company.ShouldBe("Second Vendor");
        }
    }
}
