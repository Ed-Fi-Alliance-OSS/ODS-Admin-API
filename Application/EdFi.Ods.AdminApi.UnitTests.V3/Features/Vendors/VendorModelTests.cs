// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.Vendors;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V3.Features.Vendors
{
    [TestFixture]
    public class VendorModelTests
    {
        [Test]
        public void VendorModel_DefaultValues_AreNull()
        {
            var model = new VendorModel();

            model.Id.ShouldBeNull();
            model.Company.ShouldBeNull();
            model.NamespacePrefixes.ShouldBeNull();
            model.ContactName.ShouldBeNull();
            model.ContactEmailAddress.ShouldBeNull();
        }

        [Test]
        public void VendorModel_SetProperties_ValuesAreSetCorrectly()
        {
            var model = new VendorModel
            {
                Id = 10,
                Company = "Acme Vendor",
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };

            model.Id.ShouldBe(10);
            model.Company.ShouldBe("Acme Vendor");
            model.NamespacePrefixes.ShouldBe("https://acme.org/ns");
            model.ContactName.ShouldBe("Alice");
            model.ContactEmailAddress.ShouldBe("alice@acme.org");
        }
    }
}


