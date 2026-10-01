// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditEventRecorder(
    AuditLogChannel channel,
    IOptions<AuditLoggingSettings> settings,
    IContextProvider<TenantConfiguration> tenantContextProvider,
    IConfiguration configuration,
    IOptions<AppSettings> appSettings) : IAuditEventRecorder
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(AuditEventRecorder));
    private static readonly TimeSpan _dropLogInterval = TimeSpan.FromSeconds(30);
    private static long _lastDropLogTicks;
    private static int _suppressedDropCount;

    public void Record(
        AuditEventType eventType,
        string? clientId,
        string? sourceIpAddress,
        string? httpVerb,
        string? httpUrl,
        int? statusCode,
        TenantConfiguration? tenant = null,
        string? deletedObjectSnapshot = null)
    {
        if (!settings.Value.Enabled)
        {
            return;
        }

        try
        {
            // The AsyncLocal-backed tenantContextProvider reverts for a caller once the
            // middleware that called Set() (TenantResolverMiddleware) has returned, so a
            // caller positioned outside it (AuditActionLoggingMiddleware, recording after
            // next() completes) must resolve the tenant itself and pass it in explicitly.
            tenant ??= tenantContextProvider.Get();

            // In multi-tenant mode, a null tenant here means tenant resolution itself failed
            // (e.g. TenantResolverMiddleware rejected the request for a missing/invalid Tenant
            // header, before it could set the tenant on the context). There is no per-tenant
            // connection string to fall back to, and the base "EdFi_Admin" connection string is
            // not a real, correctly-configured database in multi-tenant deployments - writing to
            // it would silently try the wrong server/engine. Log directly instead.
            if (tenant is null && appSettings.Value.MultiTenancy)
            {
                LogUnresolvedTenant(eventType, clientId, sourceIpAddress, httpVerb, httpUrl, statusCode);
                return;
            }

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
                StatusCode = statusCode,
                DeletedObjectSnapshot = deletedObjectSnapshot
            };

            if (!channel.Writer.TryWrite(auditEvent))
            {
                LogDropped(auditEvent);
            }
        }
        catch
        {
            // Audit logging must never block or fail the original request (fail-open).
            // Any failure resolving the connection string or constructing the event is
            // swallowed here.
        }
    }

    private static void LogUnresolvedTenant(
        AuditEventType eventType,
        string? clientId,
        string? sourceIpAddress,
        string? httpVerb,
        string? httpUrl,
        int? statusCode)
    {
        _logger.Error(
            "Audit event not persisted: no tenant could be resolved for this request in multi-tenant mode. " +
            $"EventType={eventType}, ClientId={clientId}, SourceIpAddress={sourceIpAddress}, " +
            $"HttpVerb={httpVerb}, HttpUrl={httpUrl}, StatusCode={statusCode}, Timestamp={DateTime.UtcNow:O}");
    }

    private static void LogDropped(AuditEvent auditEvent)
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        var lastTicks = Interlocked.Read(ref _lastDropLogTicks);
        if (nowTicks - lastTicks < _dropLogInterval.Ticks
            || Interlocked.CompareExchange(ref _lastDropLogTicks, nowTicks, lastTicks) != lastTicks)
        {
            Interlocked.Increment(ref _suppressedDropCount);
            return;
        }

        var suppressed = Interlocked.Exchange(ref _suppressedDropCount, 0);
        var suppressedNote = suppressed > 0
            ? $" ({suppressed} additional audit events dropped in the last {_dropLogInterval.TotalSeconds}s.)"
            : string.Empty;

        _logger.Error(
            $"Audit event dropped: the audit log channel is full (sustained DB outage or overload).{suppressedNote} " +
            $"EventType={auditEvent.EventType}, ClientId={auditEvent.ClientId}, " +
            $"SourceIpAddress={auditEvent.SourceIpAddress}, HttpVerb={auditEvent.HttpVerb}, " +
            $"HttpUrl={auditEvent.HttpUrl}, StatusCode={auditEvent.StatusCode}, Timestamp={auditEvent.Timestamp:O}");
    }
}
