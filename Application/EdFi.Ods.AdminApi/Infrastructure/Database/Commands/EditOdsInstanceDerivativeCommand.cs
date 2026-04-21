// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditOdsInstanceDerivativeCommand
{
    OdsInstanceDerivative Execute(IEditOdsInstanceDerivativeModel changedOdsInstanceDerivativeData);
}

public class EditOdsInstanceDerivativeCommand(IUsersContext context)
    : EditOdsInstanceDerivativeCommandBase(context), IEditOdsInstanceDerivativeCommand
{
    public OdsInstanceDerivative Execute(IEditOdsInstanceDerivativeModel changedOdsInstanceDerivativeData)
    {
        return ExecuteCore(
            changedOdsInstanceDerivativeData.Id,
            changedOdsInstanceDerivativeData.OdsInstanceId,
            changedOdsInstanceDerivativeData.DerivativeType,
            changedOdsInstanceDerivativeData.ConnectionString);
    }
}

public interface IEditOdsInstanceDerivativeModel
{
    public int Id { get; set; }
    public int OdsInstanceId { get; set; }
    public string? DerivativeType { get; set; }
    public string? ConnectionString { get; set; }
}
