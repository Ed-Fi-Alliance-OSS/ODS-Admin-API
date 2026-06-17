// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public class ConfigConnectionStringsProvider : IConfigConnectionStringsProvider
{
    private readonly IConfiguration _config;
    private readonly IContextProvider<TenantConfiguration> _tenantConfigurationContextProvider;
    private readonly IOptions<AppSettings> _options;

    public ConfigConnectionStringsProvider(
        IConfiguration config,
        IContextProvider<TenantConfiguration> tenantConfigurationContextProvider,
        IOptions<AppSettings> options)
    {
        _config = config;
        _tenantConfigurationContextProvider = tenantConfigurationContextProvider;
        _options = options;
    }

    public int Count
    {
        get => ConnectionStringProviderByName.Keys.Count;
    }

    public IDictionary<string, string> ConnectionStringProviderByName => BuildConnectionStringsByName();

    public string GetConnectionString(string name) => ConnectionStringProviderByName[name];

    private IDictionary<string, string> BuildConnectionStringsByName()
    {
        // Start from the top-level ConnectionStrings section as the base (works for both single- and multi-tenant).
        var connectionStringsByName = _config.GetSection("ConnectionStrings")
            .GetChildren()
            .ToDictionary(k => k.Key, v => v.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        if (_options.Value.MultiTenancy)
        {
            // In multi-tenant mode, each tenant has its own set of databases (EdFi_Admin, EdFi_Security,
            // EdFi_Ods, EdFi_Master). The active tenant is resolved from the ambient context, which is set
            // by TenantResolverMiddleware for HTTP requests and by CreateInstanceJob for Quartz jobs.
            // Per-tenant values override the top-level defaults when present.
            var tenantConfiguration = _tenantConfigurationContextProvider.Get();

            OverrideWhenPresent(connectionStringsByName, "EdFi_Admin", tenantConfiguration?.AdminConnectionString);
            OverrideWhenPresent(connectionStringsByName, "EdFi_Security", tenantConfiguration?.SecurityConnectionString);
            OverrideWhenPresent(connectionStringsByName, "EdFi_Ods", tenantConfiguration?.OdsConnectionString);
            OverrideWhenPresent(connectionStringsByName, "EdFi_Master", tenantConfiguration?.MasterConnectionString);
        }

        return connectionStringsByName;
    }

    private static void OverrideWhenPresent(
        IDictionary<string, string> connectionStringsByName,
        string name,
        string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            connectionStringsByName[name] = connectionString;
        }
    }
}
