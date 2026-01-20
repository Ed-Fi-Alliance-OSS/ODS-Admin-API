// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;

public interface IEducationOrganizationService
{
    Task Execute(string? tenantName);
}

public class EducationOrganizationService(
    ITenantsService tenantsService,
    IOptions<AppSettings> options,
    ITenantConfigurationProvider tenantConfigurationProvider,
    IUsersContext usersContext,
    AdminApiDbContext adminApiDbContext,
    ISymmetricStringEncryptionProvider encryptionProvider,
    IConfiguration configuration
        ) : IEducationOrganizationService
{
    private const string AllEdorgStoredProcSqlServer = "GetEducationOrganizations";
    private const string AllEdorgStoredProcPostgres = "get_education_organizations";

    public async Task Execute(string? tenantName)
    {
        var multiTenancyEnabled = options.Value.MultiTenancy;
        var encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        var databaseEngine = DatabaseEngineEnum.Parse(options.Value.DatabaseEngine ?? throw new NotFoundException<string>(nameof(AppSettings), nameof(AppSettings.DatabaseEngine)));

        if (multiTenancyEnabled)
        {
            var tenants = await GetTenantsAsync();
            var tenantConfigurations = tenantConfigurationProvider.Get();

            var tenantsWithConfiguration = tenants
                .Where(tenant => tenant.TenantName is not null &&
                                 tenant.TenantName.Equals(tenantName, StringComparison.OrdinalIgnoreCase) &&
                                 tenantConfigurations.TryGetValue(tenant.TenantName, out var config) &&
                                 config is not null)
                .Select(tenant => tenantConfigurations[tenant.TenantName!]);

            foreach (var tenantConfiguration in tenantsWithConfiguration)
            {
                await ProcessTenantConfiguration(tenantConfiguration, encryptionKey, databaseEngine);
            }
        }
        else
        {
            await ProcessOdsInstance(usersContext, adminApiDbContext, encryptionKey, databaseEngine);
        }
    }

    private async Task ProcessTenantConfiguration(TenantConfiguration tenantConfiguration, string encryptionKey, string databaseEngine)
    {
        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            var usersOptionsBuilder = new DbContextOptionsBuilder<SqlServerUsersContext>();
            usersOptionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
            usersContext = new SqlServerUsersContext(usersOptionsBuilder.Options);

            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
            adminApiDbContext = new AdminApiDbContext(adminApiOptionsBuilder.Options, configuration);

            await ProcessOdsInstance(usersContext, adminApiDbContext, encryptionKey, databaseEngine);
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            var usersOptionsBuilder = new DbContextOptionsBuilder<PostgresUsersContext>();
            usersOptionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
            usersOptionsBuilder.UseLowerCaseNamingConvention();
            usersContext = new PostgresUsersContext(usersOptionsBuilder.Options);

            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
            adminApiOptionsBuilder.UseLowerCaseNamingConvention();
            adminApiDbContext = new AdminApiDbContext(adminApiOptionsBuilder.Options, configuration);

            await ProcessOdsInstance(usersContext, adminApiDbContext, encryptionKey, databaseEngine);
        }
        else
        {
            throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
        }
    }

    public virtual async Task ProcessOdsInstance(IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine)
    {
        var odsInstances = await usersContext.OdsInstances.ToListAsync();

        foreach (var odsInstance in odsInstances)
        {
            encryptionProvider.TryDecrypt(odsInstance.ConnectionString, Convert.FromBase64String(encryptionKey), out var decryptedConnectionString);
            var connectionString = decryptedConnectionString ?? throw new InvalidOperationException("Decrypted connection string can't be null.");

            var edorgs = await GetEducationOrganizationsAsync(connectionString, databaseEngine);

            Dictionary<long, EducationOrganization>? existingEducationOrganizations;

            existingEducationOrganizations = await adminApiDbContext.EducationOrganizations
            .Where(e => e.InstanceId == odsInstance.OdsInstanceId)
            .ToDictionaryAsync(e => e.EducationOrganizationId);

            var currentSourceIds = new HashSet<long>(edorgs.Select(e => e.EducationOrganizationId));

            foreach (var edorg in edorgs)
            {
                if (existingEducationOrganizations.TryGetValue(edorg.EducationOrganizationId, out var existing))
                {
                    existing.NameOfInstitution = edorg.NameOfInstitution;
                    existing.ShortNameOfInstitution = edorg.ShortNameOfInstitution;
                    existing.Discriminator = edorg.Discriminator;
                    existing.ParentId = edorg.ParentId;
                    existing.LastModifiedDate = DateTime.UtcNow;
                    existing.LastRefreshed = DateTime.UtcNow;
                }
                else
                {
                    adminApiDbContext.EducationOrganizations.Add(new EducationOrganization
                    {
                        EducationOrganizationId = edorg.EducationOrganizationId,
                        NameOfInstitution = edorg.NameOfInstitution,
                        ShortNameOfInstitution = edorg.ShortNameOfInstitution,
                        Discriminator = edorg.Discriminator,
                        ParentId = edorg.ParentId,
                        InstanceId = odsInstance.OdsInstanceId,
                        LastModifiedDate = DateTime.UtcNow,
                        LastRefreshed = DateTime.UtcNow
                    });
                }
            }

            var educationOrganizationsToDelete = existingEducationOrganizations.Values
                .Where(e => !currentSourceIds.Contains(e.EducationOrganizationId))
                .ToList();

            if (educationOrganizationsToDelete.Count > 0)
            {
                adminApiDbContext.EducationOrganizations.RemoveRange(educationOrganizationsToDelete);
            }

            await adminApiDbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    public virtual async Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string connectionString, string databaseEngine)
    {
        if (databaseEngine is null)
            throw new InvalidOperationException("Database engine must be specified.");

        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(AllEdorgStoredProcSqlServer, connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            using var reader = await command.ExecuteReaderAsync();
            return await ReadEducationOrganizationsFromDbDataReader(reader);
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new Npgsql.NpgsqlCommand($"SELECT * FROM {AllEdorgStoredProcPostgres}()", connection);
            using var reader = await command.ExecuteReaderAsync();
            return await ReadEducationOrganizationsFromDbDataReader(reader);
        }
        else
        {
            throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
        }
    }

    private static async Task<List<EducationOrganizationResult>> ReadEducationOrganizationsFromDbDataReader(DbDataReader reader)
    {
        var results = new List<EducationOrganizationResult>();

        while (await reader.ReadAsync())
        {
            var educationOrganizationId = reader.GetInt64(reader.GetOrdinal("educationorganizationid"));
            var nameOfInstitution = reader.GetString(reader.GetOrdinal("nameofinstitution"));
            var shortNameOfInstitutionOrdinal = reader.GetOrdinal("shortnameofinstitution");
            string? shortNameOfInstitution = await reader.IsDBNullAsync(shortNameOfInstitutionOrdinal)
                ? null
                : reader.GetString(shortNameOfInstitutionOrdinal);
            var discriminator = reader.GetString(reader.GetOrdinal("discriminator"));
            var id = reader.GetGuid(reader.GetOrdinal("id"));
            var parentIdOrdinal = reader.GetOrdinal("parentid");
            long? parentId = await reader.IsDBNullAsync(parentIdOrdinal) ? null : reader.GetInt64(parentIdOrdinal);

            results.Add(new EducationOrganizationResult
            {
                EducationOrganizationId = educationOrganizationId,
                NameOfInstitution = nameOfInstitution,
                ShortNameOfInstitution = shortNameOfInstitution,
                Discriminator = discriminator,
                Id = id,
                ParentId = parentId
            });
        }

        return results;
    }

    private async Task<List<TenantModel>> GetTenantsAsync()
    {
        var tenants = await tenantsService.GetTenantsAsync();

        return tenants ?? new List<TenantModel>();
    }
}

public class EducationOrganizationResult
{
    public long EducationOrganizationId { get; set; }
    public string NameOfInstitution { get; set; } = string.Empty;
    public string? ShortNameOfInstitution { get; set; }
    public string Discriminator { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public long? ParentId { get; set; }
}
