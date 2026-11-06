// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database;

public static class ApplicationEducationOrganizationSync
{
    /// <summary>
    /// Makes the Application's education organization rows match <paramref name="requestedIds"/> by applying
    /// only the differences. Unchanged rows keep their identity and their narrowed clients' links.
    /// The caller must have loaded <paramref name="application"/> with ApplicationEducationOrganizations
    /// (and their ApiClients) and ApiClients, and calls SaveChanges itself.
    /// </summary>
    public static void Apply(IUsersContext context, Application application, IEnumerable<long>? requestedIds)
    {
        var requested = (requestedIds ?? []).Distinct().ToHashSet();

        var inheritingClients = application.ApiClients
            .Where(c => context.GetInheritsApplicationEducationOrganizations(c))
            .ToList();

        var existingRows = application.ApplicationEducationOrganizations.ToList();

        foreach (var row in existingRows.Where(r => !requested.Contains(r.EducationOrganizationId)))
        {
            // Clear the join rows explicitly so removal does not depend on the FK cascading.
            row.ApiClients.Clear();
            application.ApplicationEducationOrganizations.Remove(row);
            context.ApplicationEducationOrganizations.Remove(row);
        }

        var existingIds = existingRows.Select(r => r.EducationOrganizationId).ToHashSet();

        foreach (var id in requested.Where(id => !existingIds.Contains(id)))
        {
            application.ApplicationEducationOrganizations.Add(new ApplicationEducationOrganization
            {
                Application = application,
                EducationOrganizationId = id,
                ApiClients = inheritingClients.ToList(),
            });
        }

        // Spec 2.4: inheriting clients always hold every row; narrowed clients are never touched here.
        foreach (var row in existingRows.Where(r => requested.Contains(r.EducationOrganizationId)))
        {
            foreach (var client in inheritingClients.Where(c => !row.ApiClients.Contains(c)))
            {
                row.ApiClients.Add(client);
            }
        }
    }
}
