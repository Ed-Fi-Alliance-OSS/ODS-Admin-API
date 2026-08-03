// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
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

        var sourceIpAddress = context.Connection.RemoteIpAddress?.ToString();
        var httpVerb = context.Request.Method;
        var httpUrl = context.Request.Path.Value;

        try
        {
            await next(context);
        }
        catch
        {
            // Authentication and tenant resolution both run downstream of this middleware,
            // so context.User/context.Items are only populated by the time next() has run
            // (or thrown) - never before. The tenant must be read from context.Items rather
            // than the AsyncLocal-backed tenant context provider, since that provider's
            // value reverts once TenantResolverMiddleware's own frame returns (see
            // TenantResolverMiddleware.TenantConfigurationItemsKey).
            recorder.Record(
                AuditEventType.Action,
                context.User.FindFirst("client_id")?.Value,
                sourceIpAddress,
                httpVerb,
                httpUrl,
                StatusCodes.Status500InternalServerError,
                GetTenant(context));
            throw;
        }

        recorder.Record(
            AuditEventType.Action,
            context.User.FindFirst("client_id")?.Value,
            sourceIpAddress,
            httpVerb,
            httpUrl,
            context.Response.StatusCode,
            GetTenant(context));
    }

    private static TenantConfiguration? GetTenant(HttpContext context) =>
        context.Items.TryGetValue(TenantResolverMiddleware.TenantConfigurationItemsKey, out var tenant)
            ? tenant as TenantConfiguration
            : null;
}
