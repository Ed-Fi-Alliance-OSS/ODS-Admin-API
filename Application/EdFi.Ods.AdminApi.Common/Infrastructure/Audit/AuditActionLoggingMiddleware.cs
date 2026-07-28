// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Http;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditActionLoggingMiddleware(RequestDelegate next, IAuditEventRecorder recorder)
{
    private static readonly HashSet<string> _mutatingVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_mutatingVerbs.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        var clientId = context.User.FindFirst("client_id")?.Value;
        var sourceIpAddress = context.Connection.RemoteIpAddress?.ToString();
        var httpVerb = context.Request.Method;
        var httpUrl = context.Request.Path.Value;

        try
        {
            await next(context);
        }
        finally
        {
            var statusCode = context.Response.StatusCode is > 0 and < 600
                ? context.Response.StatusCode
                : StatusCodes.Status500InternalServerError;

            recorder.Record(AuditEventType.Action, clientId, sourceIpAddress, httpVerb, httpUrl, statusCode);
        }
    }
}
