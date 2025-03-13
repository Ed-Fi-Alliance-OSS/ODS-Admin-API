// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

public struct CommonQueryParams
{
    [FromQuery(Name = "offset")]
    public int? Offset { get; set; }
    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }
    [FromQuery(Name = "orderBy")]
    public string? OrderBy { get; set; }
    [FromQuery(Name = "direction")]
    public string? Direction { get; set; }
    [BindNever]
    public readonly bool IsDescending => SortingDirectionHelper.IsDescendingSorting(Direction);
    public CommonQueryParams() { }
    public CommonQueryParams(int? offset, int? limit)
    {
        Offset = offset;
        Limit = limit;
    }
    public CommonQueryParams(int? offset, int? limit, string? orderBy, string? direction)
    {
        Offset = offset;
        Limit = limit;
        OrderBy = orderBy;
        Direction = SortingDirectionHelper.GetNonEmptyOrDefault(direction);
    }

}

