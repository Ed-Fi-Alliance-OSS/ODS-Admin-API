// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetDbInstancesQueryBase(AdminApiDbContext context, IOptions<AppSettings> options)
{
    private readonly AdminApiDbContext _context = context;
    private readonly IOptions<AppSettings> _options = options;

    protected List<DbInstance> ExecuteCore(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        return _context.DbInstances
            .Where(d => id == null || d.Id == id)
            .Where(d => name == null || d.Name == name)
            .OrderBy(d => d.Id)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}