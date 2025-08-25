// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public interface IDeleteOdsInstanceCommand
{
    void Execute(int id);
}

public class DeleteOdsInstanceCommand : IDeleteOdsInstanceCommand
{
    private readonly IUsersContext _context;

    public DeleteOdsInstanceCommand(IUsersContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == id) ?? throw new NotFoundException<int>("odsInstance", id);
        _context.OdsInstances.Remove(odsInstance);
        _context.SaveChanges();
    }
}
