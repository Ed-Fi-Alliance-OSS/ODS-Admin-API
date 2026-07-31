// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IDeleteOdsInstanceManageCommand
{
    void Execute(int id);
}

public class DeleteOdsInstanceManageCommand : IDeleteOdsInstanceManageCommand
{
    private readonly AdminApiDbContext _context;

    public DeleteOdsInstanceManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var odsInstanceManage =
            _context.OdsInstanceManages.Find(id)
            ?? throw new NotFoundException<int>("odsInstanceManage", id);

        if (odsInstanceManage.Status != OdsInstanceManageStatus.Created.ToString())
            throw new NotFoundException<int>("odsInstanceManage", id);

        odsInstanceManage.Status = OdsInstanceManageStatus.PendingDelete.ToString();
        odsInstanceManage.LastModifiedDate = DateTime.UtcNow;

        _context.SaveChanges();
    }
}
