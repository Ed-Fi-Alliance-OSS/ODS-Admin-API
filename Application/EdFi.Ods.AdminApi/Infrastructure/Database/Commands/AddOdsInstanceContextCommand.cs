// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IAddOdsInstanceContextCommand
{
    OdsInstanceContext Execute(IAddOdsInstanceContextModel newOdsInstanceContext);
}

public class AddOdsInstanceContextCommand(IUsersContext context)
    : AddOdsInstanceContextCommandBase(context), IAddOdsInstanceContextCommand
{
    public OdsInstanceContext Execute(IAddOdsInstanceContextModel newOdsInstanceContext)
    {
        return ExecuteCore(
            newOdsInstanceContext.OdsInstanceId,
            newOdsInstanceContext.ContextKey,
            newOdsInstanceContext.ContextValue);
    }
}

public interface IAddOdsInstanceContextModel
{
    public int OdsInstanceId { get; set; }
    public string? ContextKey { get; set; }
    public string? ContextValue { get; set; }
}
