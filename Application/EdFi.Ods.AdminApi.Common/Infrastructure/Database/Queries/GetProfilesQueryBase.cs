// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetProfilesQueryBase(IUsersContext usersContext, IOptions<AppSettings> options)
{
    private readonly IUsersContext _usersContext = usersContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<Profile, object>>> _orderByColumnProfiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.ProfileName, DatabaseEngineEnum.SqlServerCollation) : x.ProfileName },
            { SortingColumns.DefaultIdColumn, x => x.ProfileId }
        };

    protected List<Profile> ExecuteCore()
    {
        return _usersContext.Profiles.OrderBy(p => p.ProfileName).ToList();
    }

    protected List<Profile> ExecuteCore(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        Expression<Func<Profile, object>> columnToOrderBy = _orderByColumnProfiles.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _usersContext.Profiles
            .Where(p => id == null || p.ProfileId == id)
            .Where(p => name == null || p.ProfileName == name)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
