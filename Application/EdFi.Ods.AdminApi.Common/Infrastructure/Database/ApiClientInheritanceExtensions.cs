// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database;

public static class ApiClientInheritanceExtensions
{
    private const string UnsupportedContextMessage =
        "IUsersContext must be AdminApiSqlServerUsersContext or AdminApiPostgresUsersContext to read InheritsApplicationEducationOrganizations.";

    public static bool GetInheritsApplicationEducationOrganizations(this IUsersContext context, ApiClient apiClient)
    {
        var dbContext = RequireDbContext(context);
        return dbContext
            .Entry(apiClient)
            .Property<bool>(ApiClientShadowProperties.InheritsApplicationEducationOrganizations)
            .CurrentValue;
    }

    public static void SetInheritsApplicationEducationOrganizations(
        this IUsersContext context,
        ApiClient apiClient,
        bool value
    )
    {
        var dbContext = RequireDbContext(context);
        dbContext
            .Entry(apiClient)
            .Property<bool>(ApiClientShadowProperties.InheritsApplicationEducationOrganizations)
            .CurrentValue = value;
    }

    public static IReadOnlyDictionary<int, bool> GetInheritsApplicationEducationOrganizations(
        this IUsersContext context,
        IEnumerable<int> apiClientIds
    )
    {
        var dbContext = RequireDbContext(context);
        var ids = apiClientIds.Distinct().ToList();

        return dbContext
            .Set<ApiClient>()
            .Where(c => ids.Contains(c.ApiClientId))
            .Select(c => new
            {
                c.ApiClientId,
                Value = EF.Property<bool>(c, ApiClientShadowProperties.InheritsApplicationEducationOrganizations)
            })
            .ToDictionary(x => x.ApiClientId, x => x.Value);
    }

    private static DbContext RequireDbContext(IUsersContext context)
    {
        if (
            context is not DbContext dbContext
            || dbContext.Model.FindEntityType(typeof(ApiClient))?.FindProperty(
                ApiClientShadowProperties.InheritsApplicationEducationOrganizations
            ) is null
        )
        {
            throw new InvalidOperationException(UnsupportedContextMessage);
        }

        return dbContext;
    }
}
