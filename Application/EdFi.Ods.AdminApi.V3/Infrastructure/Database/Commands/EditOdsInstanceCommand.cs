// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IEditOdsInstanceCommand
{
    OdsInstance Execute(IEditOdsInstanceModel changedOdsInstanceData);
}

public class EditOdsInstanceCommand(IUsersContext context) : EditOdsInstanceCommandBase(context), IEditOdsInstanceCommand
{
    public OdsInstance Execute(IEditOdsInstanceModel changedOdsInstanceData)
    {
        return ExecuteCore(changedOdsInstanceData.Id, changedOdsInstanceData.Name, changedOdsInstanceData.InstanceType, changedOdsInstanceData.ConnectionString);
    }
}

public interface IEditOdsInstanceModel
{
    public int Id { get; set; }
    string? Name { get; }
    string? InstanceType { get; }
    string? ConnectionString { get; }
}


