// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Features.EducationOrganizations;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ODSInstances;

[SwaggerSchema(Title = "OdsInstanceWithEducationOrganizations")]
public class OdsInstanceWithEducationOrganizationsModel
{
    [SwaggerSchema(Description = "ODS instance identifier", Nullable = false)]
    public int Id { get; set; }

    [SwaggerSchema(Description = "ODS instance name", Nullable = false)]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Type of ODS instance")]
    public string? InstanceType { get; set; }

    [SwaggerSchema(Description = "List of education organizations in this instance")]
    public List<EducationOrganizationModel> EducationOrganizations { get; set; } = new();
}
