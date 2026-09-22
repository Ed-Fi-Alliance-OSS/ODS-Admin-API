// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IDeleteDataStoreDerivativeCommand
{
    void Execute(int id);
}

public class DeleteDataStoreDerivativeCommand : IDeleteDataStoreDerivativeCommand
{
    private readonly IUsersContext _context;
    private readonly IDeletedEntityAuditCapture _capture;

    public DeleteDataStoreDerivativeCommand(IUsersContext context, IDeletedEntityAuditCapture capture)
    {
        _context = context;
        _capture = capture;
    }

    public void Execute(int id)
    {
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(d => d.OdsInstance)
            .SingleOrDefault(v => v.OdsInstanceDerivativeId == id) ?? throw new NotFoundException<int>("DataStoreDerivative", id);
        _capture.Record(odsInstanceDerivative);
        _context.OdsInstanceDerivatives.Remove(odsInstanceDerivative);
        _context.SaveChanges();
    }
}



