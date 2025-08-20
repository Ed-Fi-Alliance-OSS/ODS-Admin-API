// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;
public struct CommonQueryParams
{
    [FromQuery(Name = "offset")]
    public int? Offset { get; set; }
    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }
    public CommonQueryParams() { }
    public CommonQueryParams(int? offset, int? limit)
    {
        Offset = offset;
        Limit = limit;
    }
}
