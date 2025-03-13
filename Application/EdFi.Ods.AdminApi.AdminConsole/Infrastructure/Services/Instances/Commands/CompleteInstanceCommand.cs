// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
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

public interface ICompleteInstanceCommand
{
    Task<Instance> Execute(int id);
}

public class CompleteInstanceCommand(IOptions<AdminConsoleSettings> options, IUsersContext context, IQueriesRepository<Instance> instanceQuery, ICommandRepository<Instance> instanceCommand) : ICompleteInstanceCommand
{
    private readonly AdminConsoleSettings _options = options.Value;
    private readonly IUsersContext _context = context;
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;

    public static OdsInstance NewOdsInstance(Instance adminConsoleInstance)
    {
        var newOdsInstance = new OdsInstance()
        {
            Name = adminConsoleInstance.InstanceName,
            InstanceType = adminConsoleInstance.InstanceType,
            ConnectionString = string.Empty,
        };

        if (adminConsoleInstance.OdsInstanceContexts != null && adminConsoleInstance.OdsInstanceContexts.Count > 0)
        {
            newOdsInstance.OdsInstanceContexts = [];
            foreach (var adminConsoleOdsInstanceContext in adminConsoleInstance.OdsInstanceContexts)
            {
                var odsInstanceContext = new Admin.DataAccess.Models.OdsInstanceContext()
                {
                    ContextKey = adminConsoleOdsInstanceContext.ContextKey,
                    ContextValue = adminConsoleOdsInstanceContext.ContextValue,
                };
                newOdsInstance.OdsInstanceContexts.Add(odsInstanceContext);
            }
        }

        if (adminConsoleInstance.OdsInstanceDerivatives != null && adminConsoleInstance.OdsInstanceDerivatives.Count > 0)
        {
            newOdsInstance.OdsInstanceDerivatives = [];
            foreach (var adminConsoleOdsInstanceDerivatives in adminConsoleInstance.OdsInstanceDerivatives)
            {
                var odsInstanceDerivative = new Admin.DataAccess.Models.OdsInstanceDerivative()
                {
                    DerivativeType = adminConsoleOdsInstanceDerivatives.DerivativeType.ToString(),
                    ConnectionString = string.Empty
                };
                newOdsInstance.OdsInstanceDerivatives.Add(odsInstanceDerivative);
            }
        }

        return newOdsInstance;
    }

    private async Task<ApiClient> NewApiClient()
    {
        var application = await _context.Applications.Where(app => app.ApplicationName.Equals(_options.ApplicationName)).FirstAsync();

        var user = await _context.Users.Where(user => user.Vendor.VendorName.Equals(_options.VendorCompany)).FirstOrDefaultAsync();

        var apiClient = new ApiClient(true)
        {
            Name = _options.ApplicationName,
            KeyStatus = "Active",
            SecretIsHashed = false,
            Application = application,
            User = user
        };

        return apiClient;
    }

    public async Task<Instance> Execute(int id)
    {
        var transaction = _instanceCommand.BeginTransaction();

        var adminConsoleInstance = await _instanceQuery.Query().Include(w => w.OdsInstanceContexts).Include(w => w.OdsInstanceDerivatives)
            .SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);

        if (adminConsoleInstance.Status == InstanceStatus.Completed)
            return adminConsoleInstance;

        var newOdsInstance = NewOdsInstance(adminConsoleInstance);
        var newApiClient = await NewApiClient();

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

        await transaction.CommitAsync();

        return adminConsoleInstance;
    }
}
