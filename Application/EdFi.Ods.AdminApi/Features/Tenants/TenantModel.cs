// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Features.Tenants;

public class TenantModel
{
    public required string TenantName { get; set; }

    public TenantModelConnectionStrings ConnectionStrings { get; set; } = new();
}

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
