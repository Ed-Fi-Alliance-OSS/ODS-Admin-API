// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class DeleteDbInstanceCommandBase(AdminApiDbContext context)
{
    private readonly AdminApiDbContext _context = context;

    protected void ExecuteCore(int id)
    {
        var dbInstance =
            _context.DbInstances.Find(id)
            ?? throw new NotFoundException<int>("dbInstance", id);

        if (dbInstance.Status == DbInstanceStatus.Deleted.ToString())
            throw new NotFoundException<int>("dbInstance", id);

        dbInstance.Status = DbInstanceStatus.PendingDelete.ToString();
        dbInstance.LastModifiedDate = DateTime.UtcNow;

        _context.SaveChanges();
    }
}