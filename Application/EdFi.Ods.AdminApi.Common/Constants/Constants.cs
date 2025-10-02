// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Constants;

public class Constants
{
    public const string TenantsCacheKey = "tenants";
    public const string TenantNameDescription = "Admin API Tenant Name";
    public const string TenantConnectionStringDescription = "Tenant connection strings";
    public const string DefaultTenantName = "default";
}

public enum AdminApiMode
{
    V2,
    V1,
    Unversioned
}
