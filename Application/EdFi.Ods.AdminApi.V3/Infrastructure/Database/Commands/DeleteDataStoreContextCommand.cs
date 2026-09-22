// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IDeleteDataStoreContextCommand
{
    void Execute(int id);
}

public class DeleteDataStoreContextCommand : IDeleteDataStoreContextCommand
{
    private readonly IUsersContext _context;
    private readonly IDeletedEntityAuditCapture _capture;

    public DeleteDataStoreContextCommand(IUsersContext context, IDeletedEntityAuditCapture capture)
    {
        _context = context;
        _capture = capture;
    }

    public void Execute(int id)
    {
        var odsInstanceContext = _context.OdsInstanceContexts.SingleOrDefault(v => v.OdsInstanceContextId == id) ?? throw new NotFoundException<int>("DataStoreContext", id);
        _capture.Record(odsInstanceContext);
        _context.OdsInstanceContexts.Remove(odsInstanceContext);
        _context.SaveChanges();
    }
}



