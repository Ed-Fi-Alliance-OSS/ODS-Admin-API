// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

/// <summary>
/// Shared logic behind the per-version ConstantsHelpers' informational-metadata fields, so the
/// normalization algorithm and the product name live in one place instead of being duplicated
/// across the V1/V2/V3 projects.
/// </summary>
public static class ApiInformationHelper
{
    /// <summary>
    /// Application name.
    /// </summary>
    public const string ApplicationName = "Ed-Fi ODS Admin API";

    /// <summary>
    /// Strips any build-metadata suffix (e.g. "+&lt;git-sha&gt;") from the informational version so it
    /// does not leak into the response, falling back to <paramref name="fallbackVersion"/> when absent.
    /// </summary>
    public static string NormalizeInformationalVersion(string? informationalVersion, string fallbackVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return fallbackVersion;
        }

        var metadataIndex = informationalVersion.IndexOf('+');
        return metadataIndex >= 0 ? informationalVersion[..metadataIndex] : informationalVersion;
    }
}
