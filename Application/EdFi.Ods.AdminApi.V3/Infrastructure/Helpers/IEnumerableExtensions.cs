// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.


using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;

public static class IEnumerableExtensions
{
    public static string ToCommaSeparated(this IEnumerable<VendorNamespacePrefix> vendorNamespacePrefixes)
    {
        return EdFi.Ods.AdminApi.Common.Infrastructure.Helpers.IEnumerableExtensions.ToCommaSeparated(vendorNamespacePrefixes);
    }

    public static string ToDelimiterSeparated(this IEnumerable<string> inputStrings, string separator = ",")
    {
        return EdFi.Ods.AdminApi.Common.Infrastructure.Helpers.IEnumerableExtensions.ToDelimiterSeparated(inputStrings, separator);
    }

    public static IEnumerable<T> Paginate<T>(this IEnumerable<T> source, int? offset, int? limit, IOptions<AppSettings> settings)
    {
        return EdFi.Ods.AdminApi.Common.Infrastructure.Helpers.IEnumerableExtensions.Paginate(source, offset, limit, settings);
    }
}



