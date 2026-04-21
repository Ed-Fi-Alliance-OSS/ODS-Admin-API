// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetAuthStrategiesQueryBase(ISecurityContext context, IOptions<AppSettings> options)
{
    private readonly ISecurityContext _context = context;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<AuthorizationStrategy, object>>> _orderByColumnAuthorizationStrategies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.AuthorizationStrategyName, DatabaseEngineEnum.SqlServerCollation) : x.AuthorizationStrategyName },
            { SortingColumns.AuthorizationStrategyDisplayNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.DisplayName, DatabaseEngineEnum.SqlServerCollation) : x.DisplayName },
            { SortingColumns.DefaultIdColumn, x => x.AuthorizationStrategyId }
        };

    protected List<AuthorizationStrategy> ExecuteCore()
    {
        return _context.AuthorizationStrategies.OrderBy(v => v.AuthorizationStrategyName).ToList();
    }

    protected List<AuthorizationStrategy> ExecuteCore(CommonQueryParams commonQueryParams)
    {
        Expression<Func<AuthorizationStrategy, object>> columnToOrderBy = _orderByColumnAuthorizationStrategies.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _context.AuthorizationStrategies
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
