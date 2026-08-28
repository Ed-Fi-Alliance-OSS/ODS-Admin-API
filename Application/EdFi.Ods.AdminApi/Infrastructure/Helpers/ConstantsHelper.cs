// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Infrastructure.Helpers;

public static class ConstantsHelpers
{
    /// <summary>
    /// Semantic version of the admin api.
    /// </summary>
    public const string Version = "2.0";

    /// <summary>
    /// Assembly version of the admin api.
    /// </summary>
    public static readonly string Build = Assembly.GetExecutingAssembly()
        .GetName()
        .Version?.ToString() ?? Version;

    /// <summary>
    /// Application name.
    /// </summary>
    public const string ApplicationName = ApiInformationHelper.ApplicationName;

    /// <summary>
    /// Informational version description.
    /// </summary>
    public static readonly string InformationalVersion = ApiInformationHelper.NormalizeInformationalVersion(
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion,
        Version);
}
