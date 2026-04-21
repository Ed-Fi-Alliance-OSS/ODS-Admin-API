// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditApiClientCommand
{
    ApiClient Execute(IEditApiClientModel model);
}

public class EditApiClientCommand(IUsersContext context) : EditApiClientCommandBase(context), IEditApiClientCommand
{
    public ApiClient Execute(IEditApiClientModel model)
    {
        return ExecuteCore(model.Id, model.Name, model.IsApproved, model.OdsInstanceIds);
    }
}

public interface IEditApiClientModel
{
    int Id { get; }
    string Name { get; }
    bool IsApproved { get; }
    int ApplicationId { get; }
    IEnumerable<int>? OdsInstanceIds { get; }
}
