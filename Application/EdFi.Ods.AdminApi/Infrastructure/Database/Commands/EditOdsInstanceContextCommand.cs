// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditOdsInstanceContextCommand
{
    OdsInstanceContext Execute(IEditOdsInstanceContextModel changedOdsInstanceContextData);
}

public class EditOdsInstanceContextCommand(IUsersContext context)
    : EditOdsInstanceContextCommandBase(context), IEditOdsInstanceContextCommand
{
    public OdsInstanceContext Execute(IEditOdsInstanceContextModel changedOdsInstanceContextData)
    {
        return ExecuteCore(
            changedOdsInstanceContextData.Id,
            changedOdsInstanceContextData.OdsInstanceId,
            changedOdsInstanceContextData.ContextKey,
            changedOdsInstanceContextData.ContextValue);
    }
}

public interface IEditOdsInstanceContextModel
{
    public int Id { get; set; }
    public int OdsInstanceId { get; set; }
    public string? ContextKey { get; set; }
    public string? ContextValue { get; set; }
}
