// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.AdminConsole.Helpers;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IAddInstanceCommand
{
    Task<AddInstanceResult> Execute(IInstanceRequestModel instance);
}

public class AddInstanceCommand : IAddInstanceCommand
{
    private readonly ICommandRepository<Instance> _instanceCommand;

    public AddInstanceCommand(ICommandRepository<Instance> instanceCommand)
    {
        _instanceCommand = instanceCommand;
    }

    public async Task<AddInstanceResult> Execute(IInstanceRequestModel instance)
    {
        var result = await _instanceCommand.AddAsync(new Instance
        {
            OdsInstanceId = instance.OdsInstanceId,
            TenantId = instance.TenantId,
            TenantName = instance.TenantName ?? string.Empty,
            InstanceName = instance.Name ?? string.Empty,
            InstanceType = instance.InstanceType,
            Credentials = instance.Credentials,
            Status = Enum.TryParse<InstanceStatus>(instance.Status, out var status) ? status : InstanceStatus.Pending,
            OdsInstanceContexts = instance.OdsInstanceContexts?.Select(s => new OdsInstanceContext
            {
                TenantId = instance.TenantId,
                ContextKey = s.ContextKey,
                ContextValue = s.ContextValue,
            }).ToList(),
            OdsInstanceDerivatives = instance.OdsInstanceDerivatives?.Select(s => new OdsInstanceDerivative
            {
                TenantId = instance.TenantId,
                DerivativeType = Enum.Parse<DerivativeType>(s.DerivativeType, true),
            }).ToList()
        });
        return new AddInstanceResult { Id = result.Id };
    }
}

public class AddInstanceResult
{
    public int Id { get; set; }
}
