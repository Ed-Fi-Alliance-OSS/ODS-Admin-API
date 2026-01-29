// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
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
    Task Execute(string? tenantName, int? instanceId);
}

public class EducationOrganizationService(
    ITenantsService tenantsService,
    IOptions<AppSettings> options,
    ITenantConfigurationProvider tenantConfigurationProvider,
    IUsersContext usersContext,
    AdminApiDbContext adminApiDbContext,
    ISymmetricStringEncryptionProvider encryptionProvider,
    IConfiguration configuration,
    ILogger<EducationOrganizationService> logger
        ) : IEducationOrganizationService
{
    private const string AllEdorgQuery = @"
       SELECT
       edorg.educationorganizationid,
       edorg.nameofinstitution,
       edorg.shortnameofinstitution,
       edorg.discriminator,
       edorg.id,
       COALESCE(scl.localeducationagencyid, lea.parentlocaleducationagencyid, lea.educationservicecenterid, lea.stateeducationagencyid, esc.stateeducationagencyid) AS parentid
       FROM
       edfi.educationorganization edorg
       LEFT JOIN edfi.school scl
       ON
       edorg.educationorganizationid = scl.schoolid
       LEFT JOIN edfi.localeducationagency lea
       ON
       edorg.educationorganizationid = lea.localeducationagencyid
       LEFT JOIN edfi.educationservicecenter esc
       ON
       edorg.educationorganizationid = esc.educationservicecenterid
       WHERE edorg.discriminator in
       ('edfi.StateEducationAgency', 'edfi.EducationServiceCenter', 'edfi.LocalEducationAgency', 'edfi.School');
   ";

    public async Task Execute(string? tenantName, int? instanceId)
    {
        var multiTenancyEnabled = options.Value.MultiTenancy;
        var encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        var databaseEngine = DatabaseEngineEnum.Parse(options.Value.DatabaseEngine ?? throw new NotFoundException<string>(nameof(AppSettings), nameof(AppSettings.DatabaseEngine)));

        if (multiTenancyEnabled)
        {
            var tenants = await GetTenantsAsync();
            var tenantConfigurations = tenantConfigurationProvider.Get();

            var tenantsWithConfigurations = tenants
                .Where(tenant => tenant.TenantName is not null &&
                                 tenantConfigurations.TryGetValue(tenant.TenantName, out var config) &&
                                 config is not null)
                .Select(tenant => tenantConfigurations[tenant.TenantName!])
                .ToList();

            if (!string.IsNullOrEmpty(tenantName))
            {
                tenantsWithConfigurations = [.. tenantsWithConfigurations.Where(tc => !string.IsNullOrEmpty(tc.TenantIdentifier)
                && tc.TenantIdentifier.Equals(tenantName, StringComparison.OrdinalIgnoreCase))];
            }

            foreach (var tenantConfiguration in tenantsWithConfigurations)
            {
                await ProcessTenantConfiguration(tenantConfiguration, encryptionKey, databaseEngine, instanceId);
            }
        }
        else
        {
            await ProcessOdsInstance(usersContext, adminApiDbContext, encryptionKey, databaseEngine, instanceId);
        }
    }

    private async Task ProcessTenantConfiguration(TenantConfiguration tenantConfiguration, string encryptionKey, string databaseEngine, int? instanceId)
    {
        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            var usersOptionsBuilder = new DbContextOptionsBuilder<SqlServerUsersContext>();
            usersOptionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
            await using var tenantUsersContext = new SqlServerUsersContext(usersOptionsBuilder.Options);

            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
            await using var tenantAdminApiDbContext = new AdminApiDbContext(adminApiOptionsBuilder.Options, configuration);

            await ProcessOdsInstance(tenantUsersContext, tenantAdminApiDbContext, encryptionKey, databaseEngine, instanceId);
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            var usersOptionsBuilder = new DbContextOptionsBuilder<PostgresUsersContext>();
            usersOptionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
            usersOptionsBuilder.UseLowerCaseNamingConvention();
            await using var tenantUsersContext = new PostgresUsersContext(usersOptionsBuilder.Options);

            var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
            adminApiOptionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
            adminApiOptionsBuilder.UseLowerCaseNamingConvention();
            await using var tenantAdminApiDbContext = new AdminApiDbContext(adminApiOptionsBuilder.Options, configuration);

            await ProcessOdsInstance(tenantUsersContext, tenantAdminApiDbContext, encryptionKey, databaseEngine, instanceId);
        }
        else
        {
            throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
        }
    }

    public virtual async Task ProcessOdsInstance(IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine, int? instanceId)
    {
        var odsInstances = instanceId.HasValue
            ? await usersContext.OdsInstances
                .Where(o => o.OdsInstanceId == instanceId.Value)
                .ToListAsync()
            : await usersContext.OdsInstances.ToListAsync();

        // Process all OdsInstances in parallel using Task.WhenAll
        var tasks = odsInstances.Select(odsInstance =>
            ProcessSingleOdsInstanceAsync(odsInstance, encryptionKey, databaseEngine, adminApiDbContext.Database.GetConnectionString())
        );

        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleOdsInstanceAsync(OdsInstance odsInstance, string encryptionKey, string databaseEngine, string? adminConnectionString)
    {
        try
        {
            if (!encryptionProvider.TryDecrypt(odsInstance.ConnectionString, Convert.FromBase64String(encryptionKey), out var decryptedConnectionString))
            {
                logger.LogError("Failed to decrypt connection string for ODS Instance ID {OdsInstanceId}. Skipping education organization synchronization for this instance.", odsInstance.OdsInstanceId);
                return;
            }

            // Fetch education organizations from ODS database
            var edorgs = await GetEducationOrganizationsAsync(decryptedConnectionString, databaseEngine);

            // Create a new DbContext instance for this task to maintain thread safety
            await using var taskAdminApiDbContext = CreateAdminApiDbContext(adminConnectionString, databaseEngine);

            // Load existing education organizations for this instance
            var existingEducationOrganizations = await taskAdminApiDbContext.EducationOrganizations
                .Where(e => e.InstanceId == odsInstance.OdsInstanceId)
                .ToDictionaryAsync(e => e.EducationOrganizationId);

            var currentSourceIds = new HashSet<long>(edorgs.Select(e => e.EducationOrganizationId));

            // Update or add education organizations
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
                    taskAdminApiDbContext.EducationOrganizations.Add(new EducationOrganization
                    {
                        EducationOrganizationId = edorg.EducationOrganizationId,
                        NameOfInstitution = edorg.NameOfInstitution,
                        ShortNameOfInstitution = edorg.ShortNameOfInstitution,
                        Discriminator = edorg.Discriminator,
                        ParentId = edorg.ParentId,
                        InstanceId = odsInstance.OdsInstanceId,
                        InstanceName = odsInstance.Name,
                        LastModifiedDate = DateTime.UtcNow,
                        LastRefreshed = DateTime.UtcNow
                    });
                }
            }

            // Remove education organizations that no longer exist in the source
            var educationOrganizationsToDelete = existingEducationOrganizations.Values
                .Where(e => !currentSourceIds.Contains(e.EducationOrganizationId))
                .ToList();

            if (educationOrganizationsToDelete.Count > 0)
            {
                taskAdminApiDbContext.EducationOrganizations.RemoveRange(educationOrganizationsToDelete);
            }

            // Save changes for this OdsInstance
            await taskAdminApiDbContext.SaveChangesAsync(CancellationToken.None);

            logger.LogInformation("Successfully processed ODS Instance ID {OdsInstanceId}. Updated/Added {EdOrgCount} education organizations.",
                odsInstance.OdsInstanceId, edorgs.Count);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - this prevents one failure from blocking others
            logger.LogError(ex, "Error processing ODS Instance ID {OdsInstanceId}: {ErrorMessage}",
                odsInstance.OdsInstanceId, ex.Message);
        }
    }

    private AdminApiDbContext CreateAdminApiDbContext(string? connectionString, string databaseEngine)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Admin connection string cannot be null or empty.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();

        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.UseLowerCaseNamingConvention();
        }
        else
        {
            throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
        }

        return new AdminApiDbContext(optionsBuilder.Options, configuration);
    }

    public virtual async Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string? connectionString, string databaseEngine)
    {
        if (databaseEngine is null)
            throw new InvalidOperationException("Database engine must be specified.");

        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(AllEdorgQuery, connection);
            using var reader = await command.ExecuteReaderAsync();
            return await ReadEducationOrganizationsFromDbDataReader(reader);
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new Npgsql.NpgsqlCommand(AllEdorgQuery, connection);
            using var reader = await command.ExecuteReaderAsync();
            return await ReadEducationOrganizationsFromDbDataReader(reader);
        }
        else
        {
            throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
        }
    }

    private async Task<List<EducationOrganizationResult>> ReadEducationOrganizationsFromDbDataReader(DbDataReader reader)
    {
        var results = new List<EducationOrganizationResult>();

        try
        {
            while (await reader.ReadAsync())
            {
                try
                {
                    long educationOrganizationId = Convert("educationorganizationid");
                    var nameOfInstitution = reader.GetString(reader.GetOrdinal("nameofinstitution"));
                    var shortNameOfInstitutionOrdinal = reader.GetOrdinal("shortnameofinstitution");
                    string? shortNameOfInstitution = await reader.IsDBNullAsync(shortNameOfInstitutionOrdinal)
                        ? null
                        : reader.GetString(shortNameOfInstitutionOrdinal);
                    var discriminator = reader.GetString(reader.GetOrdinal("discriminator"));
                    var id = reader.GetGuid(reader.GetOrdinal("id"));
                    long? parentId = await reader.IsDBNullAsync(reader.GetOrdinal("parentid"))
                        ? null
                        : Convert("parentid");

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
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Data conversion error while reading education organizations. {Error}",
                        ex.Message
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading education organizations from database. {Error}", ex.Message);
        }
        finally
        {
            await reader.CloseAsync();
        }

        long Convert(string columnName)
        {
            var value = reader[columnName]?.ToString();

            if (!long.TryParse(value, out var educationOrganizationId))
            {
                throw new InvalidOperationException($"Invalid {columnName} value: {value}");
            }
            return educationOrganizationId;
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
