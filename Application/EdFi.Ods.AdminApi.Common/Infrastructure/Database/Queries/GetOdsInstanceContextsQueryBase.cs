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

public abstract class GetOdsInstanceContextsQueryBase(IUsersContext usersContext, IOptions<AppSettings> options)
{
    private readonly IUsersContext _usersContext = usersContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<OdsInstanceContext, object>>> _orderByColumnOds =
        CreateOrderByColumns(options);

    protected List<OdsInstanceContext> ExecuteCore()
    {
        return _usersContext.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .OrderBy(p => p.ContextKey)
            .ToList();
    }

    protected List<OdsInstanceContext> ExecuteCore(CommonQueryParams commonQueryParams)
    {
        Expression<Func<OdsInstanceContext, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _usersContext.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }

    private static Dictionary<string, Expression<Func<OdsInstanceContext, object>>> CreateOrderByColumns(IOptions<AppSettings> options)
    {
        var isSqlServerEngine = options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();

        return new Dictionary<string, Expression<Func<OdsInstanceContext, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.OdsInstanceContextKeyColumn, x => isSqlServerEngine ? EF.Functions.Collate(x.ContextKey, DatabaseEngineEnum.SqlServerCollation) : x.ContextKey },
            { SortingColumns.OdsInstanceContextValueColumn, x => isSqlServerEngine ? EF.Functions.Collate(x.ContextValue, DatabaseEngineEnum.SqlServerCollation) : x.ContextValue },
            { SortingColumns.DefaultIdColumn, x => x.OdsInstanceContextId }
        };
    }
}