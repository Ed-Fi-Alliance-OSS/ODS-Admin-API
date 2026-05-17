// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditDataStoreContextCommand
{
    OdsInstanceContext Execute(IEditDataStoreContextModel changedDataStoreContextData);
}

public class EditDataStoreContextCommand : IEditDataStoreContextCommand
{
    private readonly IUsersContext _context;

    public EditDataStoreContextCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceContext Execute(IEditDataStoreContextModel changedDataStoreContextData)
    {
        var odsInstanceContext = _context.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(v => v.OdsInstanceContextId == changedDataStoreContextData.Id) ??
            throw new NotFoundException<int>("odsInstanceContext", changedDataStoreContextData.Id);
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == changedDataStoreContextData.DataStoreId) ??
            throw new NotFoundException<int>("dataStore", changedDataStoreContextData.DataStoreId);

        odsInstanceContext.ContextKey = changedDataStoreContextData.ContextKey;
        odsInstanceContext.OdsInstance = odsInstance;
        odsInstanceContext.ContextValue = changedDataStoreContextData.ContextValue;

        _context.SaveChanges();
        return odsInstanceContext;
    }
}

public interface IEditDataStoreContextModel
{
    public int Id { get; set; }
    public int DataStoreId { get; set; }
    public string? ContextKey { get; set; }
    public string? ContextValue { get; set; }
}



