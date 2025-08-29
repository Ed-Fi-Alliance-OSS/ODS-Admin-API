// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Text.Json;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Features;

public class AdminApiModeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AdminApiMode _adminApiMode;

    public AdminApiModeValidationMiddleware(RequestDelegate next, IOptions<AppSettings> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _adminApiMode = Enum.Parse<AdminApiMode>(options.Value.AdminApiMode ?? AdminApiMode.V2.ToString(), true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var response = context.Response;
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsUnversionedEndpoint(path))
        {
            await _next(context);
            return;
        }

        var requestedVersion = GetVersionFromPath(path);

        if (requestedVersion != _adminApiMode && !path.Contains("/swagger/"))
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            await response.WriteAsync(JsonSerializer.Serialize(new { message = "Wrong API version for this instance mode." }));
            return;
        }

        await _next(context);
    }

    private static bool IsUnversionedEndpoint(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        return GetVersionFromPath(path) == AdminApiMode.Unversioned;
    }

    private static AdminApiMode GetVersionFromPath(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        return path switch
        {
            var p when p.Contains("/v2/", StringComparison.OrdinalIgnoreCase) => AdminApiMode.V2,
            var p when p.Contains("/v1/", StringComparison.OrdinalIgnoreCase) => AdminApiMode.V1,
            _ => AdminApiMode.Unversioned
        };
    }
}
