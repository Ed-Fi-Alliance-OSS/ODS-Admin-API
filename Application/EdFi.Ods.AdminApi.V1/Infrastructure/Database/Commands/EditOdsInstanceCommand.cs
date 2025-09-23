// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

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
        var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == changedOdsInstanceData.OdsInstanceId) ??
            throw new NotFoundException<int>("odsInstance", changedOdsInstanceData.OdsInstanceId);

        odsInstance.Name = changedOdsInstanceData.Name;
        odsInstance.InstanceType = changedOdsInstanceData.InstanceType;
        odsInstance.IsExtended = changedOdsInstanceData.IsExtended ?? false;
        odsInstance.Status = changedOdsInstanceData.Status;
        odsInstance.Version = changedOdsInstanceData.Version;

        _context.SaveChanges();
        return odsInstance;
    }
}

public interface IEditOdsInstanceModel
{
    public int OdsInstanceId { get; set; }
    string Name { get; }
    string InstanceType { get; }
    string Status { get; set; }
    bool? IsExtended { get; }
    string Version { get; set; }
}

