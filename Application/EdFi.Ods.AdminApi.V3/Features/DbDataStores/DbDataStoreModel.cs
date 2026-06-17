// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DbDataStores;

[SwaggerSchema(Title = "DbDataStore")]
public class DbDataStoreModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public int? DataStoreId { get; set; }
    public string? DataStoreName { get; set; }
    public string? Status { get; set; }
    public string? DatabaseTemplate { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}

