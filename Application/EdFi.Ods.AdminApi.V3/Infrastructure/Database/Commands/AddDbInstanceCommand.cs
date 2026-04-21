// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public class AddDbInstanceCommand(EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext context)
    : AddDbInstanceCommandBase(context)
{
    public DbInstance Execute(IAddDbInstanceModel model)
    {
        return ExecuteCore(model.Name, model.DatabaseTemplate);
    }
}

public interface IAddDbInstanceModel
{
    string? Name { get; }
    string? DatabaseTemplate { get; }
}



