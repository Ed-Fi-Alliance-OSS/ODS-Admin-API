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
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public interface ICompleteInstanceCommand
{
    Task<Instance> Execute(int id);
}

public class CompleteInstanceCommand(
    IOptions<AppSettings> options,
    IOptions<AdminConsoleSettings> adminConsoleOptions,
    IOptionsMonitor<TestingSettings> testingSettings,
    IUsersContext context,
    IQueriesRepository<Instance> instanceQuery,
    ICommandRepository<Instance> instanceCommand,
    ITenantConfigurationProvider tenantConfigurationProvider,
    IAdminConsoleTenantsService adminConsoleTenantsService) : ICompleteInstanceCommand
{
    private readonly AppSettings _options = options.Value;
    private readonly AdminConsoleSettings _adminConsoleOptions = adminConsoleOptions.Value;
    private readonly TestingSettings _testingSettings = testingSettings.CurrentValue;
    private readonly IUsersContext _context = context;
    private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
    private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider = tenantConfigurationProvider;
    private readonly IAdminConsoleTenantsService _adminConsoleTenantsService = adminConsoleTenantsService;

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
            string templatePrefix = "Ods_";
            newOdsInstance.ConnectionString = ConnectionStringHelper.ConnectionStringRename(databaseEngine, tenantConfiguration.AdminConnectionString, templatePrefix + adminConsoleInstance.InstanceName);
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

        //Add OAuth and ResourceUrl
        await UpdateOAuthAndResourceUrlForInstanceAsync(adminConsoleInstance);

        await _instanceCommand.UpdateAsync(adminConsoleInstance);
        await _instanceCommand.SaveChangesAsync();

        _testingSettings.CheckIfHasToThrowException();

        scope.Complete();

        return adminConsoleInstance;
    }

    private async Task UpdateOAuthAndResourceUrlForInstanceAsync(Instance instance)
    {
        var tenant = await _adminConsoleTenantsService.GetTenantByTenantIdAsync(instance.TenantId);

        if (tenant?.Document is not IDictionary<string, object> documentDictionary ||
            !documentDictionary.TryGetValue("edfiApiDiscoveryUrl", out var url) ||
            url is not string discoveryUrl)
        {
            throw new HttpRequestException("Discovery URL not found in tenant document.");
        }

        string finalUri = discoveryUrl;
        if (_options.MultiTenancy && (documentDictionary.TryGetValue("name", out var tenantName) && tenantName is string name)
            && !name.Equals("default")
            && !discoveryUrl.Contains(name))
        {
            var baseUri = new Uri(discoveryUrl);
            finalUri = new Uri(baseUri, name).ToString();
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => _options.IgnoresCertificateErrors,
            ClientCertificateOptions = ClientCertificateOption.Manual
        };

        var httpClient = new HttpClient(handler);

        var response = await httpClient.GetAsync(finalUri);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to get API URLs. Status code: {response.StatusCode}. DiscoveryURL: {discoveryUrl}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonObject = JObject.Parse(responseContent);

        var urls = jsonObject["urls"]?.ToObject<Dictionary<string, string>>() ?? jsonObject.ToObject<Dictionary<string, string>>();

        if (urls == null)
        {
            throw new HttpRequestException("Failed to extract API URLs from response.");
        }


        urls.TryGetValue("oauth", out var oauth);
        urls.TryGetValue("dataManagementApi", out var dataManagementApi);

        instance.OAuthUrl = oauth;
        instance.ResourceUrl = dataManagementApi;
    }
}
