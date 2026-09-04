// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

/// <summary>
/// Single source of truth for the Information endpoint's product-wide metadata (application name,
/// release version, build, informational version), shared across V1/V2/V3 now that all three ship
/// together as one product release rather than being versioned independently.
/// </summary>
public static class ApiInformationHelper
{
    /// <summary>
    /// Application name.
    /// </summary>
    public const string ApplicationName = "Ed-Fi ODS Admin API";

    /// <summary>
    /// Assembly version of the admin api, as stamped by the build script (VersionPrefix) from the
    /// release version/git tag.
    /// </summary>
    public static readonly string Build = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// Semantic (major.minor.build) release version of the admin api, shared across V1/V2/V3.
    /// </summary>
    public static readonly string Version = FormatVersion(Assembly.GetExecutingAssembly().GetName().Version);

    /// <summary>
    /// Informational version description.
    /// </summary>
    public static readonly string InformationalVersion = NormalizeInformationalVersion(
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion,
        Version);

    /// <summary>
    /// Drops the 4th (revision) segment .NET pads AssemblyVersion with, so the exposed version reads
    /// as a clean "major.minor.build" (e.g. "2.4.0") rather than "2.4.0.0".
    /// </summary>
    public static string FormatVersion(Version? version) => version?.ToString(3) ?? "0.0.0";

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
        var stripped = metadataIndex >= 0 ? informationalVersion[..metadataIndex] : informationalVersion;
        return string.IsNullOrWhiteSpace(stripped) ? fallbackVersion : stripped;
    }
}
