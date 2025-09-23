// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public interface IAddOdsInstanceCommand
{
    OdsInstance Execute(IAddOdsInstanceModel newOdsInstance);
}

public class AddOdsInstanceCommand : IAddOdsInstanceCommand
{
    private readonly IUsersContext _context;

    public AddOdsInstanceCommand(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstance Execute(IAddOdsInstanceModel newOdsInstance)
    {
        var odsInstance = new OdsInstance
        {
            Name = newOdsInstance.Name,
            InstanceType = newOdsInstance.InstanceType,
            Status = newOdsInstance.Status,
            IsExtended = newOdsInstance.IsExtended ?? false,
            Version = newOdsInstance.Version,
        };
        _context.OdsInstances.Add(odsInstance);
        _context.SaveChanges();
        return odsInstance;
    }
}

public interface IAddOdsInstanceModel
{
    string Name { get; }
    string InstanceType { get; }
    string Status { get; set; }
    bool? IsExtended { get; }
    string Version { get; set; }
}
