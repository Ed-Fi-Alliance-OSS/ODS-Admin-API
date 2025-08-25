// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
public static class QueryExtensions
{
    /// <summary>
    /// Apply pagination based on the offset and limit
    /// </summary>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <param name="source">IQueryable entity list to apply the pagination</param>
    /// <param name="offset"></param>
    /// <param name="limit"></param>
    /// <param name="settings">App Setting values</param>
    /// <returns>Paginated list</returns>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, int? offset, int? limit, IOptions<AppSettings> settings)
    {
        try
        {
            if (offset == null)
                offset = settings.Value.DefaultPageSizeOffset;

            if (limit == null)
                limit = settings.Value.DefaultPageSizeLimit;

            return source.Skip(offset.Value).Take(limit.Value);
        }
        catch (Exception)
        {
            // If this throws an exception simply don't paginate.
            return source;
        }
    }
}
