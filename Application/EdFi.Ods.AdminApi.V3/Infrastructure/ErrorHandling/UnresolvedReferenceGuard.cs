// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

public static class UnresolvedReferenceGuard
{
    public static void EnsureExists(bool exists, string message)
    {
        if (!exists)
        {
            throw new ConflictException(message, AdminApiProblemTypes.ConflictUnresolvedReference);
        }
    }

    public static void EnsureIdsExist<T>(IEnumerable<T>? requestedIds, IEnumerable<T> existingIds, string propertyName)
    {
        if (requestedIds is null)
        {
            return;
        }

        var requestedIdList = requestedIds.ToList();
        if (requestedIdList.Count == 0)
        {
            return;
        }

        var existingIdSet = existingIds as HashSet<T> ?? new HashSet<T>(existingIds);
        var notExist = requestedIdList.Where(id => !existingIdSet.Contains(id)).ToList();

        if (notExist.Count > 0)
        {
            throw new ConflictException(
                $"The following {propertyName} were not found in database: {string.Join(", ", notExist)}",
                AdminApiProblemTypes.ConflictUnresolvedReference
            );
        }
    }
}
