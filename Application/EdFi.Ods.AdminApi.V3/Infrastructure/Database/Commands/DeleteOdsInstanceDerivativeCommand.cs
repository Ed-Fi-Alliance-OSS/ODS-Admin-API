// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IDeleteOdsInstanceDerivativeCommand
{
    void Execute(int id);
}

public class DeleteOdsInstanceDerivativeCommand : IDeleteOdsInstanceDerivativeCommand
{
    private readonly IUsersContext _context;

    public DeleteOdsInstanceDerivativeCommand(IUsersContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var odsInstanceDerivative = _context.OdsInstanceDerivatives.SingleOrDefault(v => v.OdsInstanceDerivativeId == id) ?? throw new NotFoundException<int>("odsInstanceDerivative", id);
        _context.OdsInstanceDerivatives.Remove(odsInstanceDerivative);
        _context.SaveChanges();
    }
}



