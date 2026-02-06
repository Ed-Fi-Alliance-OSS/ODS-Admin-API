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

public class ErrorMessagesConstants
{
    /// <summary>
    /// Tenant identifier was not provided in the request header.
    /// </summary>
    /// <remarks>
    /// Use for BadRequest error messages (recommended).
    /// </remarks>
    public const string Tenant_MissingHeader = "Tenant header is missing";

    /// <summary>
    /// Tenant identifier does not have a valid format.
    /// </summary>
    public const string Tenant_InvalidFormat = "Please provide valid tenant id. Tenant id can only contain alphanumeric and -";

    /// <summary>
    /// Tenant default configuration is invalid.
    /// </summary>
    /// <remarks>
    /// Mainly when the request was made from Swagger.
    /// </remarks>
    public const string Tenant_InvalidDefault = "Please configure valid default tenant id";

    /// <summary>
    /// Tenant identifier was not found
    /// </summary>
    /// <remarks>
    /// Follow the example below to include the invalid "tenantIdentifier".
    /// <code>
    /// var message = $"{ErrorMessagesConstants.Tenant_InvalidId}: {tenantIdentifier}";
    /// </code>
    /// </remarks>
    public const string Tenant_InvalidId = "Tenant not found with provided tenant id";

    /// <summary>
    /// Tenant identifier provided in the request header does not match the tenant identifier provided as a parameter in the request URL.
    /// </summary>
    public const string Tenant_ParameterMismatch = "Tenant Header and Tenant Parameter mismatch";
}

public enum AdminApiMode
{
    V2,
    V1,
    Unversioned
}
