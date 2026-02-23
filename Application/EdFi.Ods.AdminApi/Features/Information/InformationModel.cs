// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace EdFi.Ods.AdminApi.Features.Information;

[SwaggerSchema(Title = "Information")]
public class InformationResult
{
    public InformationResult(string version, string build, TenancyResult? tenancy = null)
    {
        Build = build;
        Version = version;
        Tenancy = tenancy;
    }

    [SwaggerSchema("Tenancy information", Nullable = true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TenancyResult? Tenancy { get; }
    [SwaggerSchema("Application version", Nullable = false)]
    public string Version { get; }
    [SwaggerSchema("Build / release version", Nullable = false)]
    public string Build { get; }
}

[SwaggerSchema(Title = "Tenancy")]
public class TenancyResult
{
    public TenancyResult(bool multitenantMode, List<string> tenants)
    {
        MultitenantMode = multitenantMode;
        Tenants = tenants;
    }

    [SwaggerSchema("Indicates whether multi-tenant mode is enabled", Nullable = false)]
    public bool MultitenantMode { get; }
    [SwaggerSchema("List of available tenant names", Nullable = false)]
    public List<string> Tenants { get; }
}
