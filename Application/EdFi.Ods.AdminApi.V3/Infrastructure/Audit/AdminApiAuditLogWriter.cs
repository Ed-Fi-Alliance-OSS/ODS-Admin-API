// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Audit;

public class AdminApiAuditLogWriter(IConfiguration configuration) : IAuditLogWriter
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var engine = DatabaseEngineEnum.Parse(configuration.Get("AppSettings:DatabaseEngine", "SqlServer"));
        var optionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
        if (engine == DatabaseEngineEnum.PostgreSql)
        {
            optionsBuilder.UseNpgsql(auditEvent.AdminConnectionString);
            optionsBuilder.UseLowerCaseNamingConvention();
        }
        else
        {
            optionsBuilder.UseSqlServer(auditEvent.AdminConnectionString);
        }

        await using var context = new AdminApiDbContext(optionsBuilder.Options, configuration);
        context.AuditLogs.Add(new AuditLog
        {
            EventType = auditEvent.EventType,
            Timestamp = auditEvent.Timestamp,
            ClientId = auditEvent.ClientId,
            SourceIpAddress = auditEvent.SourceIpAddress,
            HttpVerb = auditEvent.HttpVerb,
            HttpUrl = auditEvent.HttpUrl,
            StatusCode = auditEvent.StatusCode
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
