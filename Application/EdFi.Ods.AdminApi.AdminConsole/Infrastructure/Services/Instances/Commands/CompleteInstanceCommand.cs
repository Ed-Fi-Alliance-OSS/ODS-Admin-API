// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using System.Net;
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
    IUsersContext context,
    IQueriesRepository<Instance> instanceQuery,
    ICommandRepository<Instance> instanceCommand,
    ITenantConfigurationProvider tenantConfigurationProvider,
    IAdminConsoleTenantsService adminConsoleTenantsService) : ICompleteInstanceCommand
{
    private readonly AppSettings _options = options.Value;
    private readonly AdminConsoleSettings _adminConsoleOptions = adminConsoleOptions.Value;
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

        //Add OAuth and ResourceUrl
        await UpdateOAuthAndResourceUrlForInstanceAsync(adminConsoleInstance);

        await _instanceCommand.UpdateAsync(adminConsoleInstance);
        await _instanceCommand.SaveChangesAsync();

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

        #region Uncomment this code when ODS Docker Environment is Running        
#pragma warning disable S125 // Sections of code should not be commented out
        /*
        string finalUri = discoveryUrl
        if (_options.MultiTenancy && (documentDictionary.TryGetValue("name", out var tenantName) && tenantName is string name) && !discoveryUrl.Contains(name))
        {
            var baseUri = new Uri(discoveryUrl);
            finalUri = new Uri(baseUri, name).ToString();
        }
        

        using var httpClient = new HttpClient();
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
        */
#pragma warning restore S125 // Sections of code should not be commented out
        #endregion

        #region Remove this code when ODS Docker Environment is Running
        var oAuthUri = "oauth/token";
        var resourceUri = "data/v3";
        var baseUri = new Uri(discoveryUrl);
        if (_options.MultiTenancy && (documentDictionary.TryGetValue("name", out var tenantName) && tenantName is string name) && !discoveryUrl.Contains(name))
        {
            instance.OAuthUrl = new Uri(baseUri, string.Concat(tenantName, oAuthUri)).ToString();
            instance.ResourceUrl = new Uri(baseUri, string.Concat(tenantName, resourceUri)).ToString();
            return;
        }

        instance.OAuthUrl = new Uri(baseUri, oAuthUri).ToString();
        instance.ResourceUrl = new Uri(baseUri, resourceUri).ToString();
        #endregion
    }
}
