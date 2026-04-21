// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class DeleteOdsInstanceCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected void ExecuteCore(int id)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == id)
            ?? throw new NotFoundException<int>("odsInstance", id);
        _context.OdsInstances.Remove(odsInstance);
        _context.SaveChanges();
    }
}
