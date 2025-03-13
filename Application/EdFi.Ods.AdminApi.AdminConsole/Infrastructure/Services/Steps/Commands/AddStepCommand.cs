// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Commands;

public interface IAddStepCommand
{
    Task<Step?> Execute(IAddStepModel step);
}

public class AddStepCommand(ICommandRepository<Step> stepCommand) : IAddStepCommand
{
    private readonly ICommandRepository<Step> _stepCommand = stepCommand;

    public async Task<Step?> Execute(IAddStepModel step)
    {
        try
        {
            return await _stepCommand.AddAsync(new Step
            {
                InstanceId = step.InstanceId,
                TenantId = step.TenantId,
                EdOrgId = step.EdOrgId,
                Document = step.Document,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
}

public interface IAddStepModel
{
    int InstanceId { get; }
    int? EdOrgId { get; }
    int TenantId { get; }
    string Document { get; }
}

public class AddStepResult
{
    public int DocId { get; set; }
}
