// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.HealthChecks.Commands;
public interface IAddHealthCheckCommand
{
    Task<HealthCheck?> Execute(IAddHealthCheckModel healthCheck);
}

public class AddHealthCheckCommand(ICommandRepository<HealthCheck> healtCheckCommand, IQueriesRepository<HealthCheck> healthCheckQuery) : IAddHealthCheckCommand
{
    private readonly ICommandRepository<HealthCheck> _healtCheckCommand = healtCheckCommand;
    private readonly IQueriesRepository<HealthCheck> _healthCheckQuery = healthCheckQuery;

    public async Task<HealthCheck?> Execute(IAddHealthCheckModel healthCheck)
    {
        var existingHealthCheckRow = _healthCheckQuery.Query().Where(h => h.InstanceId == healthCheck.InstanceId && h.TenantId == healthCheck.TenantId).FirstOrDefault();

        if (existingHealthCheckRow != null)
        {
            existingHealthCheckRow.Document = healthCheck.Document;
            await _healtCheckCommand.UpdateAsync(existingHealthCheckRow);
            return null;
        }
        else
        {
            return await _healtCheckCommand.AddAsync(new HealthCheck
            {
                DocId = healthCheck.DocId,
                InstanceId = healthCheck.InstanceId,
                EdOrgId = healthCheck.EdOrgId,
                TenantId = healthCheck.TenantId,
                Document = healthCheck.Document
            });
        }
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
