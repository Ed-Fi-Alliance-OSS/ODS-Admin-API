// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Applications;

[SwaggerSchema(Title = "Application")]
public class ApplicationModel
{
    public int Id { get; set; }
    public string? ApplicationName { get; set; }
    public string? ClaimSetName { get; set; }
    public IList<long>? EducationOrganizationIds { get; set; }
    public int? VendorId { get; set; }
    public IList<int>? ProfileIds { get; set; }
    public IList<int>? OdsInstanceIds { get; set; }
    public bool Enabled { get; set; } = true;
}

[SwaggerSchema(Title = "Application")]
public class SimpleApplicationModel
{
    public string? ApplicationName { get; set; }
}

[SwaggerSchema(Title = "ApplicationKeySecret")]
public class ApplicationResult
{
    public int Id { get; set; }
    public string? Key { get; set; }
    public string? Secret { get; set; }
}

[SwaggerSchema(Title = "Profile")]
public class Profile
{
    public int? Id { get; set; }
}
