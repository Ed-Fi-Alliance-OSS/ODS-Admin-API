// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IAddDataStoreContextCommand
{
    OdsInstanceContext Execute(IAddDataStoreContextModel newDataStoreContext);
}

public class AddDataStoreContextCommand : IAddDataStoreContextCommand
{
    private readonly IUsersContext _context;

    public AddDataStoreContextCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceContext Execute(IAddDataStoreContextModel newDataStoreContext)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == newDataStoreContext.DataStoreId) ??
            throw new NotFoundException<int>("dataStore", newDataStoreContext.DataStoreId);

        var context = new OdsInstanceContext
        {
            ContextKey = newDataStoreContext.ContextKey,
            ContextValue = newDataStoreContext.ContextValue,
            OdsInstance = odsInstance
        };
        _context.OdsInstanceContexts.Add(context);
        _context.SaveChanges();
        return context;
    }
}

public interface IAddDataStoreContextModel
{
    public int DataStoreId { get; set; }
    public string? ContextKey { get; set; }
    public string? ContextValue { get; set; }
}



