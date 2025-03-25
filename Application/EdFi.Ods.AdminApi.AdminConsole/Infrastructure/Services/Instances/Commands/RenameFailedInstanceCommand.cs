// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IRenameFailedInstanceCommand
{
    Task<Instance> Execute(int id);
}

public class RenameFailedInstanceCommand(IQueriesRepository<Instance> instanceQuery, ICommandRepository<Instance> instanceCommand) : IRenameFailedInstanceCommand
{
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;

    public async Task<Instance> Execute(int id)
    {
        var existingInstance = await _instanceQuery.Query().FirstOrDefaultAsync(i => i.Id == id) ?? throw new NotFoundException<int>("Instance", id);
        if (existingInstance.Status == InstanceStatus.Rename_Failed)
            return existingInstance;
        if (existingInstance.Status != InstanceStatus.Pending_Rename)
        {
            var exception = new AdminApiException(AdminConsoleValidationConstants.OdsIntanceIdStatusIsPendingRename);
            exception.StatusCode = HttpStatusCode.Conflict;
            throw exception;
        }
        existingInstance.Status = InstanceStatus.Rename_Failed;
        await _instanceCommand.UpdateAsync(existingInstance);
        return existingInstance;
    }
}
