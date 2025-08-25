// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

public static class Constants
{
    public const string ApplicationName = "Ed-Fi ODS Admin App for Suite 3";
    public const string SecurityContextApplicationName = "Ed-Fi ODS API";
    public const string VendorName = "EdFi";
    public const string VendorNamespacePrefix = "http://ed-fi.org";
    public const string InternalAdminApiClaimSet = "Ed-Fi ODS Admin App";

    public static readonly string[] SystemReservedClaimSets =
    {
        InternalAdminApiClaimSet,
        "Bootstrap Descriptors and EdOrgs"
    };

    public static readonly string[] DefaultClaimSets =
    {
        "SIS Vendor",
        "Ed-Fi Sandbox",
        "Roster Vendor",
        "Assessment Vendor",
        "Assessment Read",
        "District Hosted SIS Vendor",
        "AB Connect",
        "Bootstrap Descriptors and EdOrgs",
        "Education Preparation Program"
    };
}
