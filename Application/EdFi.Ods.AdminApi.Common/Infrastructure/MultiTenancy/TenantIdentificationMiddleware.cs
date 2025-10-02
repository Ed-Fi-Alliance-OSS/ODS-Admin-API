// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Settings;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;

public partial class TenantResolverMiddleware(
    ITenantConfigurationProvider tenantConfigurationProvider,
    IContextProvider<TenantConfiguration> tenantConfigurationContextProvider,
    IOptions<AppSettings> options,
    IOptions<SwaggerSettings> swaggerOptions) : IMiddleware
{
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider = tenantConfigurationProvider;
    private readonly IContextProvider<TenantConfiguration> _tenantConfigurationContextProvider = tenantConfigurationContextProvider;
    private readonly IOptions<AppSettings> _options = options;
    private readonly IOptions<SwaggerSettings> _swaggerOptions = swaggerOptions;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var apiMode = _options.Value.AdminApiMode?.ToLower() ?? "v2";
        var multiTenancyEnabled = _options.Value.MultiTenancy;
        var validationErrorMessage = "Please provide valid tenant id. Tenant id can only contain alphanumeric and -";

        // Check if this is a V1 endpoint
        if (IsV1Mode(apiMode))
        {
            // For V1 endpoints, skip multi-tenancy validation entirely
            await next.Invoke(context);
            return;
        }
        if (multiTenancyEnabled)
        {
            if (context.Request.Headers.TryGetValue("tenant", out var tenantIdentifier) &&
                !string.IsNullOrEmpty(tenantIdentifier))
            {
                if (IsValidTenantId(tenantIdentifier!))
                {
                    if (_tenantConfigurationProvider.Get().TryGetValue(tenantIdentifier!, out var tenantConfiguration))
                    {
                        _tenantConfigurationContextProvider.Set(tenantConfiguration);
                    }
                    else
                    {
                        ThrowTenantValidationError($"Tenant not found with provided tenant id: {tenantIdentifier}");
                    }
                }
                else
                {
                    ThrowTenantValidationError(validationErrorMessage);
                }
            }
            else if (_swaggerOptions.Value.EnableSwagger && RequestFromSwagger())
            {
                var defaultTenant = _swaggerOptions.Value.DefaultTenant;
                if (!string.IsNullOrEmpty(defaultTenant) && IsValidTenantId(defaultTenant))
                {
                    if (!string.IsNullOrEmpty(defaultTenant) &&
                        _tenantConfigurationProvider.Get().TryGetValue(defaultTenant, out var tenantConfiguration))
                    {
                        _tenantConfigurationContextProvider.Set(tenantConfiguration);
                    }
                    else
                    {
                        ThrowTenantValidationError("Please configure valid default tenant id");
                    }
                }
                else
                {
                    ThrowTenantValidationError(validationErrorMessage);
                }
            }
            else
            {
                if (!context.Request.Path.Value!.Contains("adminconsole/tenants") &&
                context.Request.Method != "GET" &&
                !context.Request.Path.Value.Contains("health", StringComparison.InvariantCultureIgnoreCase))
                {
                    ThrowTenantValidationError("Tenant header is missing (adminconsole)");
                }
            }
        }
        await next.Invoke(context);

        bool RequestFromSwagger() => (context.Request.Path.Value != null &&
            context.Request.Path.Value.Contains("swagger", StringComparison.InvariantCultureIgnoreCase)) ||
            context.Request.Headers.Referer.FirstOrDefault(x => x != null && x.ToLower().Contains("swagger", StringComparison.InvariantCultureIgnoreCase)) != null;

        void ThrowTenantValidationError(string errorMessage)
        {
            throw new ValidationException([new ValidationFailure("Tenant", errorMessage)]);
        }
    }

    private static bool IsV1Mode(string _adminApiMode)
    {
        return string.Equals(_adminApiMode, "v1", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsValidTenantId(string tenantId)
    {
        const int MaxLength = 50;
        var regex = IsValidTenantIdRegex();

        if (string.IsNullOrEmpty(tenantId) || tenantId.Length > MaxLength ||
                       !regex.IsMatch(tenantId))
        {
            return false;
        }
        return true;
    }

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex IsValidTenantIdRegex();
}
