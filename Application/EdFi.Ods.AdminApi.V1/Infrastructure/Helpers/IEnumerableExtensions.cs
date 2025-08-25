// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.


using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;

public static class IEnumerableExtensions
{
    public static string ToCommaSeparated(this IEnumerable<VendorNamespacePrefix> vendorNamespacePrefixes)
    {
        return vendorNamespacePrefixes != null && vendorNamespacePrefixes.Any()
                        ? ToDelimiterSeparated(vendorNamespacePrefixes.Select(x => x.NamespacePrefix))
                        : string.Empty;
    }

    public static string ToDelimiterSeparated(this IEnumerable<string> inputStrings, string separator = ",")
    {
        var listOfStrings = inputStrings.ToList();

        return listOfStrings.Any()
            ? string.Join(separator, listOfStrings)
            : string.Empty;
    }
}
