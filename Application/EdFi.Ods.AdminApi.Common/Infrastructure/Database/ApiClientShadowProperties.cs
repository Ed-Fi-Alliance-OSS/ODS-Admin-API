// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database;

public static class ApiClientShadowProperties
{
    public const string InheritsApplicationEducationOrganizations = "InheritsApplicationEducationOrganizations";

    public static void Configure(ModelBuilder modelBuilder, string columnName)
    {
        // No HasDefaultValue: EF would treat a CLR default of false as "unset" and never save it.
        modelBuilder
            .Entity<ApiClient>()
            .Property<bool>(InheritsApplicationEducationOrganizations)
            .HasColumnName(columnName);
    }

    public static void DefaultNewApiClientsToInheriting(object? sender, EntityTrackedEventArgs e)
    {
        if (!e.FromQuery && e.Entry.Entity is ApiClient && e.Entry.State == EntityState.Added)
        {
            e.Entry.Property(InheritsApplicationEducationOrganizations).CurrentValue = true;
        }
    }
}
