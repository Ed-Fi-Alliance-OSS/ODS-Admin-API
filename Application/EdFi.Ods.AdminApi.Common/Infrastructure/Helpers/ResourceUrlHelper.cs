// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Http;
using EdFi.Ods.AdminApi.Common.Constants;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public static class ResourceUrlHelper
{
    public static string BuildAbsoluteResourceUrl(HttpContext httpContext, AdminApiMode apiMode, string relativeResourcePath)
    {
        if (httpContext is null)
            throw new ArgumentNullException(nameof(httpContext));

        if (apiMode == AdminApiMode.Unversioned)
            throw new ArgumentException("Api mode must be versioned.", nameof(apiMode));

        if (string.IsNullOrWhiteSpace(relativeResourcePath))
            throw new ArgumentException("Resource path cannot be null or empty.", nameof(relativeResourcePath));

        var normalizedPath = relativeResourcePath.StartsWith('/')
            ? relativeResourcePath
            : $"/{relativeResourcePath}";

        var versionSegment = apiMode.ToString().ToLowerInvariant();
        var normalizedPathBase = httpContext.Request.PathBase.HasValue
            ? httpContext.Request.PathBase.Value!.TrimEnd('/')
            : string.Empty;

        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{normalizedPathBase}/{versionSegment}{normalizedPath}";
    }
}