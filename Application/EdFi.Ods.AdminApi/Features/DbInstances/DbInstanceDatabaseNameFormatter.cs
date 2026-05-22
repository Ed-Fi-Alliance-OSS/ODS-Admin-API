// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.RegularExpressions;

namespace EdFi.Ods.AdminApi.Features.DbInstances;

internal static class DbInstanceDatabaseNameFormatter
{
    private const string CanonicalPrefix = "EdFi_Ods";

    // Use PostgreSQL's identifier limit as the portable ceiling so the persisted
    // DatabaseName always matches the real provisioned database across engines.
    internal const int MaxPortableDatabaseNameLength = 63;

    private static readonly Regex _leadingCanonicalPrefixPattern = new(
        @"^(?:(?:edfi_+ods)(?:_+|$))+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static string Build(string instanceName, string databaseTemplate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseTemplate);

        var normalizedName = NormalizeSegment(instanceName);
        var normalizedDatabaseTemplate = NormalizeSegment(databaseTemplate);
        var normalizedNameWithoutPrefix = _leadingCanonicalPrefixPattern.Replace(normalizedName, string.Empty).Trim('_');

        return string.IsNullOrWhiteSpace(normalizedNameWithoutPrefix)
            ? $"{CanonicalPrefix}_{normalizedDatabaseTemplate}"
            : $"{CanonicalPrefix}_{normalizedNameWithoutPrefix}_{normalizedDatabaseTemplate}";
    }

    private static string NormalizeSegment(string value)
        => value.Replace(' ', '_').Trim('_');
}