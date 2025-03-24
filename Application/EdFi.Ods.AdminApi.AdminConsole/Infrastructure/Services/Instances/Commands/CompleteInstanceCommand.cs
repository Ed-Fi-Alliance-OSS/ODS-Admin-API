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
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface ICompleteInstanceCommand
{
    Task<Instance> Execute(int id);
}

public class CompleteInstanceCommand(
    IOptions<AppSettings> options,
    IOptions<AdminConsoleSettings> adminConsoleOptions,
    IUsersContext context,
    IQueriesRepository<Instance> instanceQuery,
    ICommandRepository<Instance> instanceCommand,
    ITenantConfigurationProvider tenantConfigurationProvider) : ICompleteInstanceCommand
{
    private readonly AppSettings _options = options.Value;
    private readonly AdminConsoleSettings _adminConsoleOptions = adminConsoleOptions.Value;
    private readonly IUsersContext _context = context;
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider = tenantConfigurationProvider;

    public async Task<Instance> Execute(int id)
    {
        var common = new InstanceCommon(_adminConsoleOptions, _context);
        var newApiClient = await common.NewApiClient();

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var adminConsoleInstance = await _instanceQuery.Query().Include(w => w.OdsInstanceContexts).Include(w => w.OdsInstanceDerivatives)
        .SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);

        if (adminConsoleInstance.Status == InstanceStatus.Completed)
            return adminConsoleInstance;

        var newOdsInstance = InstanceCommon.NewOdsInstance(adminConsoleInstance);

        var apiClientOdsInstance = new ApiClientOdsInstance()
        {
            ApiClient = newApiClient,
            OdsInstance = newOdsInstance
        };

        if (_tenantConfigurationProvider.Get().TryGetValue(adminConsoleInstance.TenantName, out TenantConfiguration? tenantConfiguration) && tenantConfiguration != null)
        {
            var databaseEngine = _options.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");
            newOdsInstance.ConnectionString = ConnectionStringHelper.ConnectionStringRename(databaseEngine, tenantConfiguration.AdminConnectionString, adminConsoleInstance.InstanceName);
        }

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
}
