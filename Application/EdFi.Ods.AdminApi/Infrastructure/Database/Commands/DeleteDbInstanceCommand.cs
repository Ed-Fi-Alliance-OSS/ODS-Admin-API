// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IDeleteDbInstanceCommand
{
    void Execute(int id);
}

public class DeleteDbInstanceCommand : IDeleteDbInstanceCommand
{
    private readonly AdminApiDbContext _context;

    public DeleteDbInstanceCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var dbInstance =
            _context.DbInstances.Find(id)
            ?? throw new ArgumentException($"DbInstance with ID {id} was not found.", nameof(id));

        dbInstance.Status = DbInstanceStatus.PendingDelete.ToString();
        dbInstance.LastModifiedDate = DateTime.UtcNow;

        _context.SaveChanges();
    }
}
