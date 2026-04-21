// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class AddDbInstanceCommandBase(AdminApiDbContext context)
{
    private readonly AdminApiDbContext _context = context;

    protected DbInstance ExecuteCore(string? name, string? databaseTemplate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(databaseTemplate))
            throw new ArgumentException("DatabaseTemplate is required.", nameof(databaseTemplate));

        var now = DateTime.UtcNow;

        var dbInstance = new DbInstance
        {
            Name = name.Trim(),
            DatabaseTemplate = databaseTemplate.Trim(),
            Status = DbInstanceStatus.Pending.ToString(),
            OdsInstanceId = null,
            OdsInstanceName = null,
            DatabaseName = null,
            LastRefreshed = now,
            LastModifiedDate = now
        };

        _context.DbInstances.Add(dbInstance);
        _context.SaveChanges();
        return dbInstance;
    }
}