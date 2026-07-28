// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditEventRecorder(
    AuditLogChannel channel,
    IOptions<AuditLoggingSettings> settings,
    IContextProvider<TenantConfiguration> tenantContextProvider,
    IConfiguration configuration) : IAuditEventRecorder
{
    public void Record(
        AuditEventType eventType,
        string? clientId,
        string? sourceIpAddress,
        string? httpVerb,
        string? httpUrl,
        int? statusCode)
    {
        if (!settings.Value.Enabled)
        {
            return;
        }

        var tenant = tenantContextProvider.Get();
        var adminConnectionString = !string.IsNullOrEmpty(tenant?.AdminConnectionString)
            ? tenant.AdminConnectionString
            : configuration.GetConnectionStringByName("EdFi_Admin");

        var auditEvent = new AuditEvent
        {
            AdminConnectionString = adminConnectionString,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            ClientId = clientId,
            SourceIpAddress = sourceIpAddress,
            HttpVerb = httpVerb,
            HttpUrl = httpUrl,
            StatusCode = statusCode
        };

        channel.Writer.TryWrite(auditEvent);
    }
}
