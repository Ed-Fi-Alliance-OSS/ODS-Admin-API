// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

/// <summary>
/// Single source of truth for the Tenancy endpoint's response shape and misconfiguration handling,
/// shared across V2/V3 (mirrors <see cref="ApiInformationHelper"/>).
/// </summary>
public static class TenancyHelper
{
    public static TenancyResult BuildTenancyResult(bool multiTenancyEnabled, IEnumerable<string> tenantNames)
    {
        if (!multiTenancyEnabled)
        {
            return new TenancyResult([]);
        }

        var tenants = tenantNames.ToList();

        if (tenants.Count == 0)
        {
            throw new AdminApiException(
                "MultiTenancy is enabled but no tenants are configured. Check the Tenants section of appsettings.")
            {
                StatusCode = HttpStatusCode.ServiceUnavailable
            };
        }

        return new TenancyResult(tenants);
    }
}

[SwaggerSchema(Title = "Tenancy")]
public class TenancyResult
{
    public TenancyResult(List<string> tenants)
    {
        Tenants = tenants;
    }

    [SwaggerSchema("List of available tenant names", Nullable = false)]
    public List<string> Tenants { get; }
}
