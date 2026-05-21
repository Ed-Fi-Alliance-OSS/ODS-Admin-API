// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Serialization;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.Tenants;

[SwaggerSchema]
public class TenantDetailModel
{
    [SwaggerSchema(Description = Constants.TenantNameDescription, Nullable = false)]
    public required string TenantName { get; set; }

    [SwaggerSchema(Title = "DataStores", Description = "List of data stores associated with the tenant")]
    public List<TenantDataStoreModel> DataStores { get; set; }

    public TenantDetailModel()
    {
        TenantName = string.Empty;
        DataStores = [];
    }
}

[SwaggerSchema]
public class TenantDataStoreModel
{
    [JsonPropertyName("id")]
    public int DataStoreId { get; set; }
    public string Name { get; set; }
    public string? DataStoreType { get; set; }

    [SwaggerSchema(Title = "EducationOrganizations")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; }

    public TenantDataStoreModel()
    {
        Name = string.Empty;
        EducationOrganizations = [];
    }
}



