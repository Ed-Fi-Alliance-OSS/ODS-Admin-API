// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using EdFi.Ods.AdminApi.Common.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Tenants;

[SwaggerSchema]
public class TenantModel
{
    [SwaggerSchema(Description = AdminConsoleConstants.TenantIdDescription, Nullable = false)]
    public int TenantId { get; set; }
    [SwaggerSchema(Description = AdminConsoleConstants.TenantIdDescription, Nullable = false, Format = "{\r\n            \"name\": \"Tenant1\",\r\n            \"edfiApiDiscoveryUrl\": \"https://api.ed-fi.org/v7.2/api\"\r\n        }")]
    public ExpandoObject? Document { get; set; }
}
