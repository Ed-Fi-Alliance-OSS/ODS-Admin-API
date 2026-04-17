// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditOdsInstanceContextCommand
{
    OdsInstanceContext Execute(IEditOdsInstanceContextModel changedOdsInstanceContextData);
}

public class EditOdsInstanceContextCommand : IEditOdsInstanceContextCommand
{
    private readonly IUsersContext _context;

    public EditOdsInstanceContextCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceContext Execute(IEditOdsInstanceContextModel changedOdsInstanceContextData)
    {
        var odsInstanceContext = _context.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(v => v.OdsInstanceContextId == changedOdsInstanceContextData.Id) ??
            throw new NotFoundException<int>("odsInstanceContext", changedOdsInstanceContextData.Id);
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == changedOdsInstanceContextData.OdsInstanceId) ??
            throw new NotFoundException<int>("odsInstance", changedOdsInstanceContextData.OdsInstanceId);

        odsInstanceContext.ContextKey = changedOdsInstanceContextData.ContextKey;
        odsInstanceContext.OdsInstance = odsInstance;
        odsInstanceContext.ContextValue = changedOdsInstanceContextData.ContextValue;

        _context.SaveChanges();
        return odsInstanceContext;
    }
}

public interface IEditOdsInstanceContextModel
{
    public int Id { get; set; }
    public int OdsInstanceId { get; set; }
    public string? ContextKey { get; set; }
    public string? ContextValue { get; set; }
}
