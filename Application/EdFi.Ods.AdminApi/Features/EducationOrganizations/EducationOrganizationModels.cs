// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.EducationOrganizations;

[SwaggerSchema(Title = "EducationOrganization")]
public class EducationOrganizationModel
{
    [SwaggerSchema(Description = "Education organization identifier", Nullable = false)]
    public long EducationOrganizationId { get; set; }

    [SwaggerSchema(Description = "Name of the education organization", Nullable = false)]
    public string NameOfInstitution { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Short name of the education organization")]
    public string? ShortNameOfInstitution { get; set; }

    [SwaggerSchema(Description = "Type of education organization (e.g., LocalEducationAgency, School)", Nullable = false)]
    public string Discriminator { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Parent education organization identifier")]
    public long? ParentId { get; set; }
}

