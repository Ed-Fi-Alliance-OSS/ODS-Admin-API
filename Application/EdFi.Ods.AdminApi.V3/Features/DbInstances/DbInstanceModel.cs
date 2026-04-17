// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DbInstances;

[SwaggerSchema(Title = "DbInstance")]
public class DbInstanceModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public int? OdsInstanceId { get; set; }
    public string? OdsInstanceName { get; set; }
    public string? Status { get; set; }
    public string? DatabaseTemplate { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
