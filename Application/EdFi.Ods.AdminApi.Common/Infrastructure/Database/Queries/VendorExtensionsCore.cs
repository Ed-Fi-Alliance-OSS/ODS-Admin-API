// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public static class VendorExtensionsCore
{
    private static readonly string[] _reservedNames =
        [
            "Ed-Fi Alliance",
            "EdFi Alliance",
            "Ed-Fi",
            "EdFi",
            "default",
            "defaultvendor"
        ];

    public static string[] ReservedNames => _reservedNames;

    public static bool IsVendorReserved(this Vendor vendor)
    {
        return _reservedNames.Contains(vendor.VendorName.Trim());
    }
}
