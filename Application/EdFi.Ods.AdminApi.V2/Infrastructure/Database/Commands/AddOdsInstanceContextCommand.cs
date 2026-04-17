// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;

public interface IAddOdsInstanceContextCommand
{
    OdsInstanceContext Execute(IAddOdsInstanceContextModel newOdsInstanceContext);
}

public class AddOdsInstanceContextCommand : IAddOdsInstanceContextCommand
{
    private readonly IUsersContext _context;

    public AddOdsInstanceContextCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceContext Execute(IAddOdsInstanceContextModel newOdsInstanceContext)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == newOdsInstanceContext.OdsInstanceId) ??
            throw new NotFoundException<int>("odsInstance", newOdsInstanceContext.OdsInstanceId);

        var odsInstanceContext = new OdsInstanceContext
        {
            ContextKey = newOdsInstanceContext.ContextKey,
            ContextValue = newOdsInstanceContext.ContextValue,
            OdsInstance = odsInstance
        };
        _context.OdsInstanceContexts.Add(odsInstanceContext);
        _context.SaveChanges();
        return odsInstanceContext;
    }
}

public interface IAddOdsInstanceContextModel
{
    public int OdsInstanceId { get; set; }
    public string? ContextKey { get; set; }
    public string? ContextValue { get; set; }
}
