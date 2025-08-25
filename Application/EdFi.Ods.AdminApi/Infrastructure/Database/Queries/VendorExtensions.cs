// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public static class VendorExtensions
{
    public static readonly string[] ReservedNames =
    {
        Constants.VendorName
    };

    public static bool IsSystemReservedVendorName(string? vendorName)
    {
        return ReservedNames.Contains(vendorName?.Trim());
    }

    public static bool IsSystemReservedVendor(this Vendor vendor)
    {
        return IsSystemReservedVendorName(vendor?.VendorName);
    }
}
