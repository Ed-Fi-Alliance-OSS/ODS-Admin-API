// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

[SwaggerSchema(Title = "OdsInstanceWithEducationOrganizations")]
public class OdsInstanceWithEducationOrganizationsModel
{
    [SwaggerSchema(Description = "ODS instance identifier", Nullable = false)]
    public int Id { get; set; }

    [SwaggerSchema(Description = "DbInstance identifier for this ODS instance")]
    public int? DbInstanceId { get; set; }

    [SwaggerSchema(Description = "ODS instance name", Nullable = false)]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Type of ODS instance")]
    public string? InstanceType { get; set; }

    [SwaggerSchema(Description = "Current provisioning status of the ODS instance")]
    public string? Status { get; set; }

    [SwaggerSchema(Description = "Database template used for this ODS instance")]
    public string? DatabaseTemplate { get; set; }

    [SwaggerSchema(Description = "Database name for this ODS instance")]
    public string? DatabaseName { get; set; }

    [SwaggerSchema(Description = "List of education organizations in this instance")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; } = new();
}
