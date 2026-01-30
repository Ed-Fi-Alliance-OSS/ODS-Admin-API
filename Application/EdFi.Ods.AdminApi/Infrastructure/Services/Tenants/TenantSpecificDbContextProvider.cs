// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;

public interface ITenantSpecificDbContextProvider
{
    AdminApiDbContext GetAdminApiDbContext(string tenantIdentifier);
    IUsersContext GetUsersContext(string tenantIdentifier);
}

public class TenantSpecificDbContextProvider : ITenantSpecificDbContextProvider
{
    private readonly IDictionary<string, TenantConfiguration> _tenantConfigurations;
    private readonly string _dbEngine;
    private readonly IConfiguration _configuration;

    public TenantSpecificDbContextProvider(ITenantConfigurationProvider tenantConfigurationProvider,
        IOptions<AppSettings> options, IConfiguration configuration)
    {
        if (options?.Value?.DatabaseEngine == null)
        {
            throw new ArgumentNullException(nameof(options), "DatabaseEngine cannot be null.");
        }

        _dbEngine = options.Value.DatabaseEngine;
        _tenantConfigurations = tenantConfigurationProvider.Get();
        _configuration = configuration;
    }

    public AdminApiDbContext GetAdminApiDbContext(string tenantIdentifier)
    {
        var tenantConfiguration = _tenantConfigurations[tenantIdentifier];

        if (_dbEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
            return new AdminApiDbContext(adminApiOptionsBuilder.Options, _configuration);
        }
        else if (_dbEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
            adminApiOptionsBuilder.UseLowerCaseNamingConvention();
            return new AdminApiDbContext(adminApiOptionsBuilder.Options, _configuration);
        }
        else
        {
            throw new NotSupportedException($"Database engine '{_dbEngine}' is not supported.");
        }
    }

    public IUsersContext GetUsersContext(string tenantIdentifier)
    {
        var tenantConfiguration = _tenantConfigurations[tenantIdentifier];

        if (_dbEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            var usersOptionsBuilder = new DbContextOptionsBuilder<SqlServerUsersContext>();
            usersOptionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
            return new SqlServerUsersContext(usersOptionsBuilder.Options);
        }
        else if (_dbEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            var usersOptionsBuilder = new DbContextOptionsBuilder<PostgresUsersContext>();
            usersOptionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
            usersOptionsBuilder.UseLowerCaseNamingConvention();
            return new PostgresUsersContext(usersOptionsBuilder.Options);
        }
        else
        {
            throw new NotSupportedException($"Database engine '{_dbEngine}' is not supported.");
        }
    }
}
