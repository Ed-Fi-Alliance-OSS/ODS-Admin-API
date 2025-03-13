// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public static class QueryStringMappingHelper
{
    public static Expression<Func<T, object>> GetColumnToOrderBy<T>(this Dictionary<string, Expression<Func<T, object>>> mapping, string? orderBy)
    {
        orderBy ??= string.Empty;

        if (mapping != null)
        {
            if (string.IsNullOrEmpty(orderBy))
                return mapping.First().Value;

            if (!mapping.TryGetValue(orderBy.ToLowerInvariant(), out Expression<Func<T, object>>? result))
            {
                throw new ValidationException([new ValidationFailure(nameof(orderBy), $"The orderBy value {orderBy} is not allowed. The allowed values are {string.Join(",", mapping.Keys)}")]);
            }

            return result;
        }

        throw new ArgumentNullException(nameof(mapping));
    }
}
