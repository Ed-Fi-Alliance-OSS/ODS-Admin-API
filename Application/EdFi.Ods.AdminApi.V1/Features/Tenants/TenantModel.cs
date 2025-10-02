// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Features.Tenants;

[SwaggerSchema]
public class TenantModel
{
    [SwaggerSchema(Description = Constants.TenantNameDescription, Nullable = false)]
    public required string TenantName { get; set; }

    [SwaggerSchema(Description = Constants.TenantConnectionStringDescription, Nullable = false)]
    public TenantModelConnectionStrings ConnectionStrings { get; set; } = new();
}

[SwaggerSchema]
public class TenantModelConnectionStrings
{
    public string EdFiSecurityConnectionString { get; set; }
    public string EdFiAdminConnectionString { get; set; }

    public TenantModelConnectionStrings()
    {
        EdFiAdminConnectionString = string.Empty;
        EdFiSecurityConnectionString = string.Empty;
    }

    public TenantModelConnectionStrings(string edFiAdminConnectionString, string edFiSecurityConnectionString)
    {
        EdFiAdminConnectionString = edFiAdminConnectionString;
        EdFiSecurityConnectionString = edFiSecurityConnectionString;
    }
}
