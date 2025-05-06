// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Dynamic;
using System.Transactions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IDeletedInstanceCommand
{
    Task Execute(int id);
}

public class DeletedInstanceCommand : IDeletedInstanceCommand
{
    private readonly IUsersContext _context;
    private readonly IQueriesRepository<Instance> _instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand;
    private readonly TestingSettings _testingSettings;

    public DeletedInstanceCommand(IUsersContext context, IQueriesRepository<Instance> instanceQuery, ICommandRepository<Instance> instanceCommand, IOptionsMonitor<TestingSettings> testingSettings)
    {
        _context = context;
        _instanceQuery = instanceQuery;
        _instanceCommand = instanceCommand;
        _testingSettings = testingSettings.CurrentValue;
    }

    public async Task Execute(int id)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        var adminConsoleInstance = await _instanceQuery.Query().SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);
        try
        {
            if (adminConsoleInstance.Status == InstanceStatus.Deleted)
                return;

            var odsInstanceId = adminConsoleInstance.OdsInstanceId;

            var odsInstance = await _context.OdsInstances
                .Include(i => i.OdsInstanceContexts)
                .Include(i => i.OdsInstanceDerivatives)
                .SingleOrDefaultAsync(w => w.OdsInstanceId == odsInstanceId);

            var apiclientOdsInstance = _context.ApiClientOdsInstances.Include(i => i.ApiClient).Where(w => w.OdsInstance.OdsInstanceId == odsInstanceId);

            if (apiclientOdsInstance != null)
            {
                _context.ApiClientOdsInstances.RemoveRange(apiclientOdsInstance);
                _context.ApiClients.RemoveRange(apiclientOdsInstance.Select(p => p.ApiClient).Distinct());
            }
            if (odsInstance != null)
            {
                _context.OdsInstanceContexts.RemoveRange(odsInstance.OdsInstanceContexts);
                _context.OdsInstanceDerivatives.RemoveRange(odsInstance.OdsInstanceDerivatives);
                _context.OdsInstances.Remove(odsInstance);
            }
            _context.SaveChanges();

            adminConsoleInstance.Status = InstanceStatus.Deleted;

            _testingSettings.CheckIfHasToThrowException();

            await _instanceCommand.UpdateAsync(adminConsoleInstance);
            scope.Complete();
        }
        catch (Exception)
        {
            adminConsoleInstance.Status = InstanceStatus.Delete_Failed;
            await _instanceCommand.UpdateAsync(adminConsoleInstance);
            throw;
        }
    }
}
