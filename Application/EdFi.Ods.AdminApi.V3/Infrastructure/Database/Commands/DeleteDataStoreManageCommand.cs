// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IDeleteDataStoreManageCommand
{
    void Execute(int id);
}

public class DeleteDataStoreManageCommand : IDeleteDataStoreManageCommand
{
    private readonly AdminApiDbContext _context;

    public DeleteDataStoreManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var odsInstanceManage =
            _context.OdsInstanceManages.Find(id)
            ?? throw new NotFoundException<int>("dataStoreManage", id);

        if (odsInstanceManage.Status == OdsInstanceManageStatus.Deleted.ToString())
            throw new NotFoundException<int>("dataStoreManage", id);

        odsInstanceManage.Status = OdsInstanceManageStatus.PendingDelete.ToString();
        odsInstanceManage.LastModifiedDate = DateTime.UtcNow;

        _context.SaveChanges();
    }
}
