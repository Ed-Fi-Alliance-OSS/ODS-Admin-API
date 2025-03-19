// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IPendingDeleteInstanceCommand
{
    Task Execute(int id);
}

public class PendingDeleteInstanceCommand(IQueriesRepository<Instance> instanceQuery, ICommandRepository<Instance> instanceCommand) : IPendingDeleteInstanceCommand
{
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;

    public async Task Execute(int id)
    {
        var existingInstance = await _instanceQuery.Query().FirstOrDefaultAsync(i => i.Id == id);
        if (existingInstance == null)
        {
            throw new NotFoundException<int>("odsInstance", id);
        }
        if (existingInstance.Status != InstanceStatus.Completed)
        {
            throw new ValidationException([new ValidationFailure(nameof(id), AdminConsoleValidationConstants.OdsIntanceIdStatusIsNotCompleted)]);
        }
        existingInstance.Status = InstanceStatus.Pending_Delete;
        await _instanceCommand.UpdateAsync(existingInstance);
    }
}
