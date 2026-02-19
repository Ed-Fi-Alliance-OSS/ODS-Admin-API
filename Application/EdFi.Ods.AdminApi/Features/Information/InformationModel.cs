// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Information;

[SwaggerSchema(Title = "Information")]
public class InformationResult
{
    public InformationResult(string version, string build, string tenantMode)
    {
        Build = build;
        Version = version;
        TenantMode = tenantMode;
    }

    [SwaggerSchema("Application version", Nullable = false)]
    public string Version { get; }
    [SwaggerSchema("Build / release version", Nullable = false)]
    public string Build { get; }
    [SwaggerSchema("Tenant mode (multitenant or singletenant)", Nullable = false)]
    public string TenantMode { get; }
}
