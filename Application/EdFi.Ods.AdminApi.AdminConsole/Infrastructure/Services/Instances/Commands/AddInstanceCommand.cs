// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IAddInstanceCommand
{
    Task<AddInstanceResult> Execute(IInstanceRequestModel instance);
}

public class AddInstanceCommand(ICommandRepository<Instance> instanceCommand) : IAddInstanceCommand
{
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;

    public async Task<AddInstanceResult> Execute(IInstanceRequestModel instance)
    {
        var result = await _instanceCommand.AddAsync(Instance.From(instance));
        return new AddInstanceResult { Id = result.Id };
    }
}

public class AddInstanceResult
{
    public int Id { get; set; }
}
