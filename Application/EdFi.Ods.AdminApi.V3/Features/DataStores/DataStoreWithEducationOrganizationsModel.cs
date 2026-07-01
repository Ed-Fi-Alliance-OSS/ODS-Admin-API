// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

[SwaggerSchema(Title = "DataStoreWithEducationOrganizations")]
public class DataStoreWithEducationOrganizationsModel
{
    [SwaggerSchema(Description = "Data store identifier", Nullable = false)]
    public int Id { get; set; }

    [SwaggerSchema(Description = "Data store name", Nullable = false)]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Type of data store")]
    public string? DataStoreType { get; set; }

    [SwaggerSchema(Description = "Current provisioning status of the data store")]
    public string? Status { get; set; }

    [SwaggerSchema(Description = "Database template used for this data store")]
    public string? DatabaseTemplate { get; set; }

    [SwaggerSchema(Description = "Database name for this data store")]
    public string? DatabaseName { get; set; }

    [SwaggerSchema(Description = "List of education organizations in this data store")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; } = new();
}
