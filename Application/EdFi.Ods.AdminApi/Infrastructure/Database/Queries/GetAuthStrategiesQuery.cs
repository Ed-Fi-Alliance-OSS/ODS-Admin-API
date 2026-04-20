// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetAuthStrategiesQuery
{
    List<AuthorizationStrategy> Execute();
    List<AuthorizationStrategy> Execute(CommonQueryParams commonQueryParams);
}

public class GetAuthStrategiesQuery : IGetAuthStrategiesQuery
{
    private readonly ISecurityContext _context;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<AuthorizationStrategy, object>>> _orderByColumnAuthorizationStrategies;
    public GetAuthStrategiesQuery(ISecurityContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();

        _orderByColumnAuthorizationStrategies = new Dictionary<string, Expression<Func<AuthorizationStrategy, object>>>
            (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.AuthorizationStrategyName, DatabaseEngineEnum.SqlServerCollation) : x.AuthorizationStrategyName },
            { SortingColumns.AuthorizationStrategyDisplayNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.DisplayName, DatabaseEngineEnum.SqlServerCollation) : x.DisplayName },
            { SortingColumns.DefaultIdColumn, x => x.AuthorizationStrategyId }
        };
    }

    public List<AuthorizationStrategy> Execute()
    {
        return _context.AuthorizationStrategies.OrderBy(v => v.AuthorizationStrategyName).ToList();
    }

    public List<AuthorizationStrategy> Execute(CommonQueryParams commonQueryParams)
    {
        Expression<Func<AuthorizationStrategy, object>> columnToOrderBy = _orderByColumnAuthorizationStrategies.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _context.AuthorizationStrategies
        .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
        .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
        .ToList();
    }
}

