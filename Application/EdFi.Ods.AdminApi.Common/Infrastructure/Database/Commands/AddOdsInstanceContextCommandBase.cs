// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class AddOdsInstanceContextCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected OdsInstanceContext ExecuteCore(int odsInstanceId, string? contextKey, string? contextValue)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == odsInstanceId) ??
            throw new NotFoundException<int>("odsInstance", odsInstanceId);

        var odsInstanceContext = new OdsInstanceContext
        {
            ContextKey = contextKey,
            ContextValue = contextValue,
            OdsInstance = odsInstance
        };

        _context.OdsInstanceContexts.Add(odsInstanceContext);
        _context.SaveChanges();
        return odsInstanceContext;
    }
}