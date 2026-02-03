// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Serialization;
using EdFi.Ods.AdminApi.Common.Constants;
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

    [SwaggerSchema(Title = "EducationOrganization")]
    public List<TenantEducationOrganizationModel> EducationOrganizations { get; set; }

    public TenantOdsInstanceModel()
    {
        Name = string.Empty;
        EducationOrganizations = [];
    }
}

[SwaggerSchema]
public class TenantEducationOrganizationModel
{
    public int InstanceId { get; set; }
    public string InstanceName { get; set; }
    public long EducationOrganizationId { get; set; }
    public string NameOfInstitution { get; set; }
    public string? ShortNameOfInstitution { get; set; }
    public string Discriminator { get; set; }
    public long? ParentId { get; set; }

    public TenantEducationOrganizationModel()
    {
        InstanceName = string.Empty;
        NameOfInstitution = string.Empty;
        ShortNameOfInstitution = string.Empty;
        Discriminator = string.Empty;
    }
}
