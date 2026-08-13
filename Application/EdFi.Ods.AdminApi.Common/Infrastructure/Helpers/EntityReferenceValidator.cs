// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using FluentValidation;
using FluentValidation.Results;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public static class EntityReferenceValidator
{
    public static void ValidateIdsExist<T>(IEnumerable<T>? requestedIds, IEnumerable<T> existingIds, string propertyName)
    {
        if (requestedIds is null || !requestedIds.Any())
        {
            return;
        }

        var existingIdSet = existingIds as HashSet<T> ?? new HashSet<T>(existingIds);

        if (existingIdSet.Count == 0)
        {
            throw new ValidationException(new[] { new ValidationFailure(propertyName, $"The following {propertyName} were not found in database: {string.Join(", ", requestedIds)}") });
        }

        var notExist = requestedIds.Where(id => !existingIdSet.Contains(id)).ToList();
        if (notExist.Count > 0)
        {
            throw new ValidationException(new[] { new ValidationFailure(propertyName, $"The following {propertyName} were not found in database: {string.Join(", ", notExist)}") });
        }
    }
}
