// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public class AddOdsInstanceManageCommand
{
    private readonly AdminApiDbContext _context;

    public AddOdsInstanceManageCommand(AdminApiDbContext context)
    {
        _context = context;
    }

    public OdsInstanceManage Execute(IAddOdsInstanceManageModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ArgumentException("Name is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(model.DatabaseTemplate))
            throw new ArgumentException("DatabaseTemplate is required.", nameof(model));

        var now = DateTime.UtcNow;

        var odsInstanceManage = new OdsInstanceManage
        {
            Name = model.Name.Trim(),
            DatabaseTemplate = model.DatabaseTemplate.Trim(),
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            OdsInstanceId = null,
            OdsInstanceName = null,
            DatabaseName = null,
            LastRefreshed = now,
            LastModifiedDate = now
        };

        _context.OdsInstanceManages.Add(odsInstanceManage);
        _context.SaveChanges();
        return odsInstanceManage;
    }
}

public interface IAddOdsInstanceManageModel
{
    string? Name { get; }
    string? DatabaseTemplate { get; }
}
