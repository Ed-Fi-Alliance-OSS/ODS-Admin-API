// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Dynamic;
using System.Net;
using System.Transactions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IDeleteInstanceFailedCommand
{
    Task<Instance> Execute(int id);
}

public class DeleteInstanceFailedCommand : IDeleteInstanceFailedCommand
{
    private readonly IQueriesRepository<Instance> _instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand;

    public DeleteInstanceFailedCommand(IQueriesRepository<Instance> instanceQuery, ICommandRepository<Instance> instanceCommand)
    {
        _instanceQuery = instanceQuery;
        _instanceCommand = instanceCommand;
    }

    public async Task<Instance> Execute(int id)
    {
        var adminConsoleInstance = await _instanceQuery.Query()
            .SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);

        if (adminConsoleInstance.Status == InstanceStatus.Delete_Failed)
            return adminConsoleInstance;
        else if (adminConsoleInstance.Status != InstanceStatus.Pending_Delete)
        {
            var adminApiException = new AdminApiException(AdminConsoleValidationConstants.OdsInstanceIdStatusIsNotPendingDelete);
            adminApiException.StatusCode = HttpStatusCode.Conflict;
            throw adminApiException;
        }
        adminConsoleInstance.Status = InstanceStatus.Delete_Failed;
        await _instanceCommand.UpdateAsync(adminConsoleInstance);
        return adminConsoleInstance;
    }
}
