// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Extensions
{
    public static class QueryExtensions
    {
        /// <summary>
        /// Ordering the IQueryable base in the expression
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="source">DBSet that contains the data</param>
        /// <param name="orderBy">Expression function that contains the column to order</param>
        /// <param name="isDescending">Indicate if it is descending</param>
        /// <returns></returns>
        public static IQueryable<T> OrderByColumn<T>(this IQueryable<T> source, Expression<Func<T, object>> orderBy, bool isDescending)
        {
            if (isDescending)
                source = source.OrderByDescending(orderBy);
            else
                source = source.OrderBy(orderBy);
            return source;
        }

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
}
