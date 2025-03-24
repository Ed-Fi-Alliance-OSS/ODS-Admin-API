// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Security.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Security.PgSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.AdminConsole.Configurations;

public static class AdminConsoleExtension
{
    public static void UseCorsForAdminConsole(this WebApplication app)
    {
        var adminConsoleSettings = app.Services.GetService<IOptions<AdminConsoleSettings>>();

        if (adminConsoleSettings != null && adminConsoleSettings.Value.CorsSettings.EnableCors)
            app.UseCors(AdminConsoleConstants.CorsPolicyName);
    }

    public static async Task InitAdminConsoleData(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        int applicationId = 0;
        IAdminConsoleTenantsService? adminConsoleTenantsService = scope.ServiceProvider.GetService<IAdminConsoleTenantsService>();
        if (adminConsoleTenantsService != null)
        {
            //Get tenants
            await adminConsoleTenantsService.InitializeTenantsAsync();
            //Populate intances
            var options = scope.ServiceProvider.GetService<IOptionsSnapshot<AppSettingsFile>>();
            var tenantId = 1;
            if (options!.Value.AppSettings.MultiTenancy)
            {
                foreach (var item in options!.Value.Tenants)
                {
                    using var scopeInstances = app.Services.CreateScope();
                    var tenantConfigurationProvider = scopeInstances.ServiceProvider.GetService<ITenantConfigurationProvider>();
                    var tenantConfigurationContextProvider = scopeInstances.ServiceProvider.GetService<IContextProvider<TenantConfiguration>>();
                    if (tenantConfigurationProvider!.Get().TryGetValue(item.Key, out var tenantConfiguration))
                    {
                        //assign connection string to the dbcontext when is multitenant
                        tenantConfigurationContextProvider!.Set(tenantConfiguration);
                        IAdminConsoleInitializationService? adminConsoleInitializationService = scopeInstances.ServiceProvider.GetService<IAdminConsoleInitializationService>();
                        IAdminConsoleInstancesService? adminConsoleInstancesService = scopeInstances.ServiceProvider.GetService<IAdminConsoleInstancesService>();
                        if (adminConsoleInitializationService != null)
                        {
                            applicationId = await adminConsoleInitializationService.InitializeApplications(app);
                        }

                        if (adminConsoleInstancesService != null)
                            await adminConsoleInstancesService.InitializeInstancesAsync(tenantId, applicationId);
                        tenantId++;
                    }
                }
            }
            else
            {
                //single tenant
                //get instances in adminconsole
                IAdminConsoleInstancesService? adminConsoleInstancesService = scope.ServiceProvider.GetService<IAdminConsoleInstancesService>();
                IAdminConsoleInitializationService? adminConsoleInitializationService = scope.ServiceProvider.GetService<IAdminConsoleInitializationService>();
                if (adminConsoleInstancesService != null)
                {
                    if (adminConsoleInitializationService != null)
                        applicationId = await adminConsoleInitializationService.InitializeApplications(app);
                    await adminConsoleInstancesService.InitializeInstancesAsync(tenantId, applicationId);
                }
            }
        }
    }

    public static void MigrateSecurityDbContext(this WebApplication app)
    {
        DbContext dbContext;
        var databaseEngine = DbProviders.Parse(app.Configuration.GetValue<string>("AppSettings:DatabaseEngine")!);
        using var scope = app.Services.CreateScope();
        var options = scope.ServiceProvider.GetService<IOptionsSnapshot<AppSettingsFile>>();
        if (options!.Value.AppSettings.MultiTenancy)
        {
            foreach (var item in options!.Value.Tenants)
            {
                var tenantConfigurationProvider = scope.ServiceProvider.GetService<ITenantConfigurationProvider>();
                if (tenantConfigurationProvider!.Get().TryGetValue(item.Key, out var tenantConfiguration))
                {
                    switch (databaseEngine)
                    {
                        case DbProviders.SqlServer:
                            dbContext = scope.ServiceProvider.GetRequiredService<AdminConsoleSecurityMsSqlContext>();
                            dbContext.Database.SetConnectionString(tenantConfiguration.SecurityConnectionString);
                            dbContext.Database.Migrate();
                            break;
                        case DbProviders.PostgreSql:
                            dbContext = scope.ServiceProvider.GetRequiredService<AdminConsoleSecurityPgSqlContext>();
                            dbContext.Database.SetConnectionString(tenantConfiguration.SecurityConnectionString);
                            dbContext.Database.Migrate();
                            break;
                    }
                }
            }
        }
        else
        {
            switch (databaseEngine)
            {
                case DbProviders.SqlServer:
                    dbContext = scope.ServiceProvider.GetRequiredService<AdminConsoleSecurityMsSqlContext>();
                    dbContext.Database.Migrate();
                    break;
                case DbProviders.PostgreSql:
                    dbContext = scope.ServiceProvider.GetRequiredService<AdminConsoleSecurityPgSqlContext>();
                    dbContext.Database.Migrate();
                    break;
            }
        }
    }
}
