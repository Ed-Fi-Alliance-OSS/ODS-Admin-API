// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using log4net;
using Microsoft.Extensions.Hosting;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLogBackgroundService(AuditLogChannel channel, IAuditLogWriter writer) : BackgroundService
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(AuditLogBackgroundService));
    private static readonly TimeSpan[] _retryDelays = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditEvent in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessEventAsync(auditEvent, writer, stoppingToken);
        }
    }

    internal async Task<bool> ProcessEventAsync(AuditEvent auditEvent, IAuditLogWriter eventWriter, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= _retryDelays.Length; attempt++)
        {
            try
            {
                await eventWriter.WriteAsync(auditEvent, cancellationToken);
                return false;
            }
            catch (Exception ex) when (attempt < _retryDelays.Length)
            {
                _logger.Warn($"Audit log write failed (attempt {attempt + 1}), retrying.", ex);

                try
                {
                    await Task.Delay(_retryDelays[attempt], cancellationToken);
                }
                catch (OperationCanceledException cancelEx)
                {
                    LogFallback(auditEvent, cancelEx);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogFallback(auditEvent, ex);
                return true;
            }
        }

        return true;
    }

    private static void LogFallback(AuditEvent auditEvent, Exception ex)
    {
        _logger.Error(
            $"Audit log write failed after {_retryDelays.Length + 1} attempts; falling back to text log. " +
            $"EventType={auditEvent.EventType}, ClientId={auditEvent.ClientId}, " +
            $"SourceIpAddress={auditEvent.SourceIpAddress}, HttpVerb={auditEvent.HttpVerb}, " +
            $"HttpUrl={auditEvent.HttpUrl}, StatusCode={auditEvent.StatusCode}, Timestamp={auditEvent.Timestamp:O}",
            ex);
    }
}
