// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.HealthChecks.Commands;
public interface IAddHealthCheckCommand
{
    Task<HealthCheck> Execute(IAddHealthCheckModel newHealthCheck);
}

public class AddHealthCheckCommand(ICommandRepository<HealthCheck> healtCheckCommand) : IAddHealthCheckCommand
{
    private readonly ICommandRepository<HealthCheck> _healtCheckCommand = healtCheckCommand;

    public async Task<HealthCheck> Execute(IAddHealthCheckModel newHealthCheck)
    {
        return await _healtCheckCommand.AddAsync(new HealthCheck
        {
            DocId = newHealthCheck.DocId,
            InstanceId = newHealthCheck.InstanceId,
            EdOrgId = newHealthCheck.EdOrgId,
            TenantId = newHealthCheck.TenantId,
            Document = newHealthCheck.Document
        });
    }
}
public interface IAddHealthCheckModel
{
    int DocId { get; }
    int InstanceId { get; }
    int EdOrgId { get; }
    int TenantId { get; }
    string Document { get; }
}
