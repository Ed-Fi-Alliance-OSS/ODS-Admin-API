// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

[SwaggerSchema(Title = "DataStore")]
public class DataStoreModel
{
    [JsonPropertyName("id")]
    public int DataStoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DataStoreType { get; set; }
}

[SwaggerSchema(Title = "DataStoreDetail")]
public class DataStoreDetailModel : DataStoreModel
{
    public IEnumerable<DataStoreContextModel>? DataStoreContexts { get; set; }
    public IEnumerable<DataStoreDerivativeModel>? DataStoreDerivatives { get; set; }
}
