// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IAddApiClientCommand
{
    AddApiClientResult Execute(IAddApiClientModel apiClientModel, IOptions<AppSettings> options);
}

public class AddApiClientCommand(IUsersContext usersContext) : AddApiClientCommandBase(usersContext), IAddApiClientCommand
{
    public AddApiClientResult Execute(IAddApiClientModel apiClientModel, IOptions<AppSettings> options)
    {
        return ExecuteCore(apiClientModel.Name, apiClientModel.IsApproved, apiClientModel.ApplicationId, apiClientModel.OdsInstanceIds, options);
    }
}

public interface IAddApiClientModel
{
    string Name { get; }
    bool IsApproved { get; }
    int ApplicationId { get; }
    IEnumerable<int>? OdsInstanceIds { get; }
}
