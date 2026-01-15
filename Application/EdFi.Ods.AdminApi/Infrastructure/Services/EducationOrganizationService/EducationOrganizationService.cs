// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Database;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;

public interface IEducationOrganizationService
{
    Task Execute();
}

public class EducationOrganizationService : IEducationOrganizationService
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
       on
       edorg.educationorganizationid = esc.educationservicecenterid
       WHERE edorg.discriminator in
       ('edfi.StateEducationAgency', 'edfi.EducationServiceCenter', 'edfi.LocalEducationAgency', 'edfi.School');
   ";

    private readonly ITenantsService _tenantsService;
    private readonly IOptions<AppSettings> _options;
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider;
    private readonly IAdminApiUserContext _adminApiUsersContext;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider;

    public EducationOrganizationService(
        ITenantsService tenantsService,
        IOptions<AppSettings> options,
        ITenantConfigurationProvider tenantConfigurationProvider,
        IAdminApiUserContext adminApiUsersContext,
        ISymmetricStringEncryptionProvider encryptionProvider
        )
    {
        _tenantsService = tenantsService;
        _options = options;
        _tenantConfigurationProvider = tenantConfigurationProvider;
        _adminApiUsersContext = adminApiUsersContext;
        _encryptionProvider = encryptionProvider;
    }

    public async Task Execute()
    {
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var encryptionKey = _options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        var databaseEngine = DatabaseEngineEnum.Parse(_options.Value.DatabaseEngine ?? throw new NotFoundException<string>(nameof(AppSettings), nameof(AppSettings.DatabaseEngine)));

        if (multiTenancyEnabled)
        {
            var tenants = await GetTenantsAsync();
            foreach (var tenant in tenants)
            {
                if (_tenantConfigurationProvider.Get().TryGetValue(tenant.TenantName!, out var tenantConfiguration) && tenantConfiguration != null)
                {
                    if (databaseEngine.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>();
                        optionsBuilder.UseSqlServer(tenantConfiguration.AdminConnectionString);
                        var adminApiUsersContext = new AdminConsoleSqlServerUsersContext(optionsBuilder.Options);

                        await ProcessOdsInstance(adminApiUsersContext, encryptionKey, databaseEngine);
                    }
                    else if (databaseEngine.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<AdminConsolePostgresUsersContext>();
                        optionsBuilder.UseNpgsql(tenantConfiguration.AdminConnectionString);
                        var adminApiUsersContext = new AdminConsolePostgresUsersContext(optionsBuilder.Options);

                        await ProcessOdsInstance(adminApiUsersContext, encryptionKey, databaseEngine);
                    }
                    else
                    {
                        throw new NotSupportedException($"Database engine '{databaseEngine}' is not supported.");
                    }
                }
            }
        }
        else
        {
            await ProcessOdsInstance(_adminApiUsersContext, encryptionKey, databaseEngine);
        }
    }

    public virtual async Task ProcessOdsInstance(IAdminApiUserContext context, string encryptionKey, string databaseEngine)
    {
        var odsInstances = await context.OdsInstances.ToListAsync();
        foreach (var odsInstance in odsInstances)
        {
            _encryptionProvider.TryDecrypt(odsInstance.ConnectionString, Convert.FromBase64String(encryptionKey), out var decryptedConnectionString);
            var connectionString = decryptedConnectionString ?? throw new InvalidOperationException("Decrypted connection string can't be null.");

            var edorgs = await GetEducationOrganizationsAsync(connectionString, databaseEngine);

            Dictionary<long, EducationOrganization>? existingEducationOrganizations;

            existingEducationOrganizations = await context.EducationOrganizations
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
                    context.EducationOrganizations.Add(new EducationOrganization
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
                context.EducationOrganizations.RemoveRange(educationOrganizationsToDelete);
            }

            await context.SaveChangesAsync();
        }
    }

    public virtual async Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string connectionString, string databaseEngine)
    {
        if (databaseEngine is null)
            throw new InvalidOperationException("Database engine must be specified.");

        if (databaseEngine.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(AllEdorgQuery, connection);
            using var reader = await command.ExecuteReaderAsync();
            return await ReadEducationOrganizationsFromDbDataReader(reader);
        }
        else if (databaseEngine.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
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
        var tenants = await _tenantsService.GetTenantsAsync();

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
