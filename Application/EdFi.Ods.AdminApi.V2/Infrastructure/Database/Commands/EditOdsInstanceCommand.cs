
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;

public interface IEditOdsInstanceCommand
{
    OdsInstance Execute(IEditOdsInstanceModel changedOdsInstanceData);
}

public class EditOdsInstanceCommand : IEditOdsInstanceCommand
{
    private readonly IUsersContext _context;

    public EditOdsInstanceCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstance Execute(IEditOdsInstanceModel changedOdsInstanceData)
    {
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == changedOdsInstanceData.Id) ??
            throw new NotFoundException<int>("odsInstance", changedOdsInstanceData.Id);

        odsInstance.Name = changedOdsInstanceData.Name;
        odsInstance.InstanceType = changedOdsInstanceData.InstanceType;
        if (!string.IsNullOrEmpty(changedOdsInstanceData.ConnectionString))
            odsInstance.ConnectionString = changedOdsInstanceData.ConnectionString;

        _context.SaveChanges();
        return odsInstance;
    }
}

public interface IEditOdsInstanceModel
{
    public int Id { get; set; }
    string? Name { get; }
    string? InstanceType { get; }
    string? ConnectionString { get; }
}

