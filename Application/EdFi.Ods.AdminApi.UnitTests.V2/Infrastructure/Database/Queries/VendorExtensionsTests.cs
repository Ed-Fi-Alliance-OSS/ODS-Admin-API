// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V2.Infrastructure.Database.Queries
{
    [TestFixture]
    public class VendorExtensionsTests
    {
        [Test]
        public void IsSystemReservedVendorName_ReturnsTrue_ForReservedName()
        {
            var reservedName = VendorExtensions.ReservedNames[0];

            VendorExtensions.IsSystemReservedVendorName(reservedName).ShouldBeTrue();
            VendorExtensions.IsSystemReservedVendorName($" {reservedName} ").ShouldBeTrue();
        }

        [Test]
        public void IsSystemReservedVendorName_ReturnsFalse_ForNonReservedName()
        {
            VendorExtensions.IsSystemReservedVendorName("Custom Vendor").ShouldBeFalse();
            VendorExtensions.IsSystemReservedVendorName(null).ShouldBeFalse();
        }

        [Test]
        public void IsSystemReservedVendor_ReturnsExpectedValue_ForVendorInstance()
        {
            var reservedVendor = new Vendor { VendorName = VendorExtensions.ReservedNames[0] };
            var customVendor = new Vendor { VendorName = "Custom Vendor" };

            reservedVendor.IsSystemReservedVendor().ShouldBeTrue();
            customVendor.IsSystemReservedVendor().ShouldBeFalse();
        }
    }
}
