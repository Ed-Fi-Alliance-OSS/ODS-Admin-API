// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Serialization;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Features.EducationOrganizations;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Tenants;

[SwaggerSchema]
public class TenantDetailModel
{
    [SwaggerSchema(Description = Constants.TenantNameDescription, Nullable = false)]
    public required string TenantName { get; set; }

    [SwaggerSchema(Title = "OdsInstance")]
    public List<TenantOdsInstanceModel> OdsInstances { get; set; }

    public TenantDetailModel()
    {
        TenantName = string.Empty;
        OdsInstances = [];
    }
}

[SwaggerSchema]
public class TenantOdsInstanceModel
{
    [JsonPropertyName("id")]
    public int OdsInstanceId { get; set; }
    public string Name { get; set; }
    public string? InstanceType { get; set; }

    [SwaggerSchema(Title = "EducationOrganizations")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; }

    public TenantOdsInstanceModel()
    {
        Name = string.Empty;
        EducationOrganizations = [];
    }
}
