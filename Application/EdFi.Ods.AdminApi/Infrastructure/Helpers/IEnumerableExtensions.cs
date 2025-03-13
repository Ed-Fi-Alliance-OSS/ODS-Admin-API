// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.


using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Helpers;

public static class IEnumerableExtensions
{
    public static string ToCommaSeparated(this IEnumerable<VendorNamespacePrefix> vendorNamespacePrefixes)
    {
        return vendorNamespacePrefixes != null && vendorNamespacePrefixes.Any()
                        ? ToDelimiterSeparated([.. vendorNamespacePrefixes.Select(x => x.NamespacePrefix).OrderBy(f => f)])
                        : string.Empty;
    }

    public static string ToDelimiterSeparated(this IEnumerable<string> inputStrings, string separator = ",")
    {
        var listOfStrings = inputStrings.ToList();

        return listOfStrings.Count != 0
            ? string.Join(separator, listOfStrings)
            : string.Empty;
    }

    public static IEnumerable<T> Paginate<T>(this IEnumerable<T> source, int? offset, int? limit, IOptions<AppSettings> settings)
    {
        try
        {
            offset ??= settings.Value.DefaultPageSizeOffset;

            limit ??= settings.Value.DefaultPageSizeLimit;

            return source.Skip(offset.Value).Take(limit.Value);
        }
        catch (Exception)
        {
            // If this throws an exception simply don't paginate.
            return source;
        }
    }
}
