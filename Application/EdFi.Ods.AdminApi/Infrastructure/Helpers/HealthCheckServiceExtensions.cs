// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Settings;

namespace EdFi.Ods.AdminApi.Infrastructure;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddHealthCheck(
        this IServiceCollection services,
        IConfigurationRoot configuration
    )
    {
        var databaseEngine = configuration.Get("AppSettings:DatabaseEngine", "SqlServer");
        var multiTenancyEnabled = configuration.Get("AppSettings:MultiTenancy", false);

        if (!string.IsNullOrEmpty(databaseEngine))
        {
            var isSqlServer = DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer);
            var hcBuilder = services.AddHealthChecks();

            // Add health checks for both EdFi_Admin and EdFi_Security databases
            AddDatabaseHealthChecks(hcBuilder, configuration, "EdFi_Admin", multiTenancyEnabled, isSqlServer);
            AddDatabaseHealthChecks(hcBuilder, configuration, "EdFi_Security", multiTenancyEnabled, isSqlServer);
        }

        return services;
    }

    private static void AddDatabaseHealthChecks(
        IHealthChecksBuilder hcBuilder,
        IConfigurationRoot configuration,
        string connectionStringName,
        bool multiTenancyEnabled,
        bool isSqlServer
    )
    {
        Dictionary<string, string> connectionStrings;

        if (multiTenancyEnabled)
        {
            var tenantSettings =
                configuration.Get<TenantsSection>()
                ?? throw new AdminApiException("Unable to load tenant configuration from appSettings");

            connectionStrings = tenantSettings.Tenants.ToDictionary(
                x => x.Key,
                x => x.Value.ConnectionStrings[connectionStringName]
            );
        }
        else
        {
            connectionStrings = new()
            {
                { "SingleTenant", configuration.GetConnectionStringByName(connectionStringName) }
            };
        }

        foreach (var connectionString in connectionStrings)
        {
            var healthCheckName = multiTenancyEnabled
                ? $"{connectionString.Key}_{connectionStringName}"
                : connectionStringName;

            if (isSqlServer)
            {
                hcBuilder.AddSqlServer(connectionString.Value, name: healthCheckName, tags: ["Databases"]);
            }
            else
            {
                hcBuilder.AddNpgSql(connectionString.Value, name: healthCheckName, tags: ["Databases"]);
            }
        }
    }
}
