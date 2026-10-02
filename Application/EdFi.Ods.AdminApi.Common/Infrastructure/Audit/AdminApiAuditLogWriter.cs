// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Concurrent;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AdminApiAuditLogWriter(IConfiguration configuration) : IAuditLogWriter
{
    // AdminApiAuditLogWriter is registered as a singleton, so this cache lives for the app's
    // lifetime. Each entry caches the immutable EF provider configuration (connection string +
    // provider/retry setup) for one connection string (one per tenant), built once instead of
    // on every audit write. Rebuilding a DbContextOptionsBuilder/connection string fresh per
    // write is what caused Npgsql's NpgsqlConnectionStringBuilder to race with other concurrent
    // DB activity in the process. Connection pooling/recovery is handled independently by the
    // ADO.NET provider (SqlClient/Npgsql) based on the connection string, not by this cache, so
    // there is nothing here that needs to expire.
    private readonly ConcurrentDictionary<string, DbContextOptions<AdminApiDbContext>> _optionsCache = new();

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var options = GetOrBuildOptions(auditEvent.AdminConnectionString);

        await using var context = new AdminApiDbContext(options, configuration);
        context.AuditLogs.Add(new AuditLog
        {
            EventType = auditEvent.EventType,
            Timestamp = auditEvent.Timestamp,
            ClientId = auditEvent.ClientId,
            SourceIpAddress = auditEvent.SourceIpAddress,
            HttpVerb = auditEvent.HttpVerb,
            HttpUrl = auditEvent.HttpUrl,
            StatusCode = auditEvent.StatusCode,
            DeletedObjectSnapshot = auditEvent.DeletedObjectSnapshot
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    internal DbContextOptions<AdminApiDbContext> GetOrBuildOptions(string connectionString) =>
        _optionsCache.GetOrAdd(connectionString, BuildOptions);

    private DbContextOptions<AdminApiDbContext> BuildOptions(string connectionString)
    {
        var engine = DatabaseEngineEnum.Parse(configuration.Get("AppSettings:DatabaseEngine", "SqlServer"));
        var optionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
        if (engine == DatabaseEngineEnum.PostgreSql)
        {
            optionsBuilder.UseNpgsql(
                connectionString,
                o => o.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null));
            optionsBuilder.UseLowerCaseNamingConvention();
        }
        else
        {
            optionsBuilder.UseSqlServer(
                connectionString,
                o => o.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null));
        }

        return optionsBuilder.Options;
    }
}
