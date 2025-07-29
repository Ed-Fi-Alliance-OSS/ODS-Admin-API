// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ApiClients;

[SwaggerSchema(Title = "ApiClient")]
public class ApiClientModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
    public bool UseSandbox { get; set; } = false;
    public int SandboxType { get; set; } = 0;
    public int ApplicationId { get; set; } = 0;
    public string KeyStatus { get; set; } = "Active";
    public IList<long>? EducationOrganizationIds { get; set; }
    public IList<int>? OdsInstanceIds { get; set; }
}

[SwaggerSchema(Title = "ApiClient")]
public class ApiClientResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
}
