// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database;

public static class EntityFrameworkCoreDatabaseModelBuilderExtensions
{
    public static void ApplyDatabaseServerSpecificConventions(this ModelBuilder modelBuilder, string databaseEngine)
    {
        if ("SqlServer".Equals(databaseEngine, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity is null)
                throw new InvalidOperationException("Entity should not be null");
            var tableName = entity.GetTableName() ?? throw new InvalidOperationException($"Entity of type {entity.GetType()} has a null table name");

            entity.SetTableName(tableName.ToLowerInvariant());

            foreach (var property in entity.GetProperties())
            {
                var tableId = StoreObjectIdentifier.Table(tableName);
                var columnName = property.GetColumnName(tableId) ?? property.GetDefaultColumnName(tableId);
                if (columnName != null)
                    property.SetColumnName(columnName.ToLowerInvariant());
            }

            foreach (var key in entity.GetKeys())
                key.SetName(key.GetName()?.ToLowerInvariant());

            foreach (var key in entity.GetForeignKeys())
                key.SetConstraintName(key.GetConstraintName()?.ToLowerInvariant());

            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(index.GetDatabaseName()?.ToLowerInvariant());
        }
    }
}
