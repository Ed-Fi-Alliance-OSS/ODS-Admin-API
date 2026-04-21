// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IDeleteApiClientCommand
{
    void Execute(int id);
}

public class DeleteApiClientCommand(IUsersContext context) : DeleteApiClientCommandBase(context), IDeleteApiClientCommand
{
    public void Execute(int id)
    {
        ExecuteCore(id);
    }
}
