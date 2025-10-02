// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.Features.Tenants;

public class ReadTenants : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/tenants", GetTenantsAsync)
            .BuildForVersions(AdminApiVersions.V1);
    }

    public static IResult GetTenantsAsync(IOptions<AppSettings> options, IOptions<AppSettingsFile> _appSettings)
    {
        const string ADMIN_DB_KEY = "EdFi_Admin";
        const string SECURITY_DB_KEY = "EdFi_Security";
        var _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

        var defaultTenant = new TenantModel()
        {
            TenantName = Common.Constants.Constants.DefaultTenantName,
            ConnectionStrings = new TenantModelConnectionStrings
                (
                    edFiAdminConnectionString: _appSettings.Value.ConnectionStrings.First(p => p.Key == ADMIN_DB_KEY).Value,
                    edFiSecurityConnectionString: _appSettings.Value.ConnectionStrings.First(p => p.Key == SECURITY_DB_KEY).Value
                )
        };

        var adminHostAndDatabase = ConnectionStringHelper.GetHostAndDatabase(_databaseEngine, defaultTenant.ConnectionStrings.EdFiAdminConnectionString);
        var securityHostAndDatabase = ConnectionStringHelper.GetHostAndDatabase(_databaseEngine, defaultTenant.ConnectionStrings.EdFiSecurityConnectionString);

        var response = new TenantsResponse
        {
            TenantName = defaultTenant.TenantName,
            AdminConnectionString = new EdfiConnectionString()
            {
                host = adminHostAndDatabase.Host,
                database = adminHostAndDatabase.Database
            },
            SecurityConnectionString = new EdfiConnectionString()
            {
                host = securityHostAndDatabase.Host,
                database = securityHostAndDatabase.Database
            }
        };
        return Results.Ok(response);
    }
}

public class TenantsResponse
{
    public string? TenantName { get; set; }
    public EdfiConnectionString? AdminConnectionString { get; set; }
    public EdfiConnectionString? SecurityConnectionString { get; set; }
}

public class EdfiConnectionString
{
    public string? host { get; set; }
    public string? database { get; set; }
}
