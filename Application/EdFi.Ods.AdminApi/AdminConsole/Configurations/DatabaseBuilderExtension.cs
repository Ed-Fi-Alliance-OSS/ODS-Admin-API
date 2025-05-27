// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.PgSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Security.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Security.PgSql;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole;

public static class DatabaseBuilderExtension
{
    public static void ConfigureAdminConsoleDatabase(this WebApplicationBuilder webApplicationBuilder)
    {
        IConfiguration config = webApplicationBuilder.Configuration;

        var databaseEngine = DbProviders.Parse(config.GetValue<string>("AppSettings:DatabaseEngine")!);
        var multiTenancyEnabled = config.Get("AppSettings:MultiTenancy", false);

        switch (databaseEngine)
        {
            case DbProviders.SqlServer:
                /// Admin
                webApplicationBuilder.Services.AddDbContext<IDbContext, AdminConsoleMsSqlContext>(
                    (sp, options) =>
                    {
                        options.UseSqlServer(
                            AdminConnection(sp).AdminConnectionString,
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                        );
                    }
                );

                /// Security
                webApplicationBuilder.Services.AddDbContext<AdminConsoleSecurityMsSqlContext>(
                    (sp, options) =>
                    {
                        options.UseSqlServer(
                            AdminConnection(sp).SecurityConnectionString,
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                        );
                    }
                );
                break;
            case DbProviders.PostgreSql:
                /// Admin
                webApplicationBuilder.Services.AddDbContext<IDbContext, AdminConsolePgSqlContext>(
                    (sp, options) =>
                    {
                        options.UseNpgsql(
                            AdminConnection(sp).AdminConnectionString,
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                        );
                    }
                );

                /// Security
                webApplicationBuilder.Services.AddDbContext<AdminConsoleSecurityPgSqlContext>(
                    (sp, options) =>
                    {
                        options.UseNpgsql(
                            AdminConnection(sp).SecurityConnectionString,
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                        );
                    }
                );
                break;
            default:
                throw new ArgumentException(
                    $"Unexpected DB setup error. Engine '{databaseEngine}' was parsed as valid but is not configured for startup."
                );
        }

        TenantConfiguration AdminConnection(IServiceProvider serviceProvider)
        {
            var connection = new TenantConfiguration();
            if (multiTenancyEnabled)
            {
                var tenant = serviceProvider
                    .GetRequiredService<IContextProvider<TenantConfiguration>>()
                    .Get();
                if (
                    tenant != null
                    && !string.IsNullOrEmpty(tenant.AdminConnectionString)
                    && !string.IsNullOrEmpty(tenant.SecurityConnectionString)
                )
                {
                    connection.AdminConnectionString = tenant.AdminConnectionString;
                    connection.SecurityConnectionString = tenant.SecurityConnectionString;
                }
                else
                {
                    throw new ArgumentException(
                        $"Admin database connection setup error. Tenant not configured correctly."
                    );
                }
            }
            else
            {
                connection.AdminConnectionString = config.GetConnectionStringByName("EdFi_Admin");
                connection.SecurityConnectionString = config.GetConnectionStringByName("EdFi_Security");
            }

            return connection;
        }
    }
}
