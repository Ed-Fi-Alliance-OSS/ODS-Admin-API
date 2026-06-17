// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;

public class TenantConfiguration
{
    public string? TenantIdentifier { get; set; }

    public string? AdminConnectionString { get; set; }

    public string? SecurityConnectionString { get; set; }

    public string? OdsConnectionString { get; set; }

    public string? MasterConnectionString { get; set; }
}
