// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using System.Transactions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface IRenameInstanceCommand
{
    Task<Instance> Execute(int id, string connectionString);
}

public class RenameInstanceCommand(
    IOptions<AdminConsoleSettings> adminConsoleOptions,
    IUsersContext context,
    IQueriesRepository<Instance> instanceQuery,
    ICommandRepository<Instance> instanceCommand) : IRenameInstanceCommand
{
    private readonly AdminConsoleSettings _adminConsoleOptions = adminConsoleOptions.Value;
    private readonly IUsersContext _context = context;
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;

    public async Task<Instance> Execute(int id, string connectionString)
    {
        var common = new InstanceCommon(_adminConsoleOptions, _context);
        var newApiClient = await common.NewApiClient();

        var adminConsoleInstance = await _instanceQuery.Query().Include(w => w.OdsInstanceContexts).Include(w => w.OdsInstanceDerivatives)
        .SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);

        if (adminConsoleInstance.Status == InstanceStatus.Completed)
            return adminConsoleInstance;

        if (adminConsoleInstance.Status != InstanceStatus.Pending_Rename)
            throw new AdminApiException($"Invalid operation on instance with name {adminConsoleInstance.InstanceName}.")
            {
                StatusCode = System.Net.HttpStatusCode.Conflict
            };

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            /// Droping if necessary
            if (adminConsoleInstance.OdsInstanceId > 0)
            {
                var odsInstanceId = adminConsoleInstance.OdsInstanceId;

                var odsInstance = await _context.OdsInstances
                    .Include(i => i.OdsInstanceContexts)
                    .Include(i => i.OdsInstanceDerivatives)
                    .SingleOrDefaultAsync(w => w.OdsInstanceId == odsInstanceId);

                var apiclientOdsInstance = await _context.ApiClientOdsInstances
                    .Include(i => i.ApiClient)
                    .SingleOrDefaultAsync(w => w.OdsInstance.OdsInstanceId == odsInstanceId);

                if (apiclientOdsInstance != null)
                {
                    _context.ApiClientOdsInstances.Remove(apiclientOdsInstance);
                    _context.ApiClients.Remove(apiclientOdsInstance.ApiClient);
                }
                if (odsInstance != null)
                {
                    _context.OdsInstanceContexts.RemoveRange(odsInstance.OdsInstanceContexts);
                    _context.OdsInstanceDerivatives.RemoveRange(odsInstance.OdsInstanceDerivatives);
                    _context.OdsInstances.Remove(odsInstance);
                }
            }

            /// Recreating
            var newOdsInstance = InstanceCommon.NewOdsInstance(adminConsoleInstance);
            newOdsInstance.ConnectionString = connectionString;

            var apiClientOdsInstance = new ApiClientOdsInstance()
            {
                ApiClient = newApiClient,
                OdsInstance = newOdsInstance
            };

            _context.ApiClients.Add(newApiClient);
            _context.OdsInstances.Add(newOdsInstance);
            _context.ApiClientOdsInstances.Add(apiClientOdsInstance);
            _context.SaveChanges();

            adminConsoleInstance.OdsInstanceId = newOdsInstance.OdsInstanceId;
            adminConsoleInstance.Status = InstanceStatus.Completed;

            dynamic apiCredentials = new ExpandoObject();
            apiCredentials.ClientId = newApiClient.Key;
            apiCredentials.Secret = newApiClient.Secret;
            adminConsoleInstance.Credentials = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(apiCredentials));

            await _instanceCommand.UpdateAsync(adminConsoleInstance);
            await _instanceCommand.SaveChangesAsync();

            scope.Complete();

            return adminConsoleInstance;
        }
        catch (Exception)
        {
            adminConsoleInstance.Status = InstanceStatus.Rename_Failed;
            await _instanceCommand.SaveChangesAsync();
            throw;
        }
    }
}
