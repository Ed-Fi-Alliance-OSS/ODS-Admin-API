// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public class AddDbInstanceCommand
{
    private readonly AdminApiDbContext _context;

    public AddDbInstanceCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public DbInstance Execute(IAddDbInstanceModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ArgumentException("Name is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(model.DatabaseTemplate))
            throw new ArgumentException("DatabaseTemplate is required.", nameof(model));

        var now = DateTime.UtcNow;

        var dbInstance = new DbInstance
        {
            Name = model.Name.Trim(),
            DatabaseTemplate = model.DatabaseTemplate.Trim(),
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

public interface IAddDbInstanceModel
{
    string? Name { get; }
    string? DatabaseTemplate { get; }
}



