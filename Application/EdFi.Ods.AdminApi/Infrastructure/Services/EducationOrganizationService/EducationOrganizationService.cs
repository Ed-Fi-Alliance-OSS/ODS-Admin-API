// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
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
    IOptions<AppSettings> options,
    IUsersContext usersContext,
    AdminApiDbContext adminApiDbContext,
    ISymmetricStringEncryptionProvider encryptionProvider,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    IServiceScopeFactory serviceScopeFactory,
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
            if (string.IsNullOrEmpty(tenantName))
            {
                logger.LogError("Tenant name must be provided when multi-tenancy is enabled.");
                return;
            }
            var tenantSpecificAdminApiDbContext = tenantSpecificDbContextProvider.GetAdminApiDbContext(tenantName!);
            var tenantSpecificUsersContext = tenantSpecificDbContextProvider.GetUsersContext(tenantName!);
            await ProcessOdsInstanceAsync(tenantName, tenantSpecificUsersContext, tenantSpecificAdminApiDbContext, encryptionKey, databaseEngine);
        }
        else
        {
            await ProcessOdsInstanceAsync("default", usersContext, adminApiDbContext, encryptionKey, databaseEngine, instanceId);
        }
    }

    public virtual async Task ProcessOdsInstanceAsync(string tenantName, IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine, int? instanceId = null)
    {
        var odsInstances = instanceId.HasValue
            ? await usersContext.OdsInstances
                .Where(o => o.OdsInstanceId == instanceId.Value)
                .ToListAsync()
            : await usersContext.OdsInstances.ToListAsync();

        // Process all OdsInstances in parallel using Task.WhenAll
        var tasks = odsInstances.Select(odsInstance =>
            RefreshEducationOrganizationsAsync(tenantName, encryptionKey, databaseEngine, odsInstance)
        );

        await Task.WhenAll(tasks);
    }

    private async Task RefreshEducationOrganizationsAsync(string tenantName, string encryptionKey, string databaseEngine, Admin.DataAccess.Models.OdsInstance odsInstance)
    {
        try
        {
            if (!encryptionProvider.TryDecrypt(odsInstance.ConnectionString, Convert.FromBase64String(encryptionKey), out var decryptedConnectionString))
            {
                logger.LogError("Failed to decrypt connection string for ODS Instance ID {OdsInstanceId}. Skipping education organization synchronization for this instance.", odsInstance.OdsInstanceId);
            }

            var edorgs = await GetEducationOrganizationsAsync(decryptedConnectionString, databaseEngine);

            // Create a completely isolated scope with its own DbContext instance for thread safety
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider.GetRequiredService<ITenantSpecificDbContextProvider>();
            var taskAdminApiDbContext = scopedProvider.GetAdminApiDbContext(tenantName);

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
