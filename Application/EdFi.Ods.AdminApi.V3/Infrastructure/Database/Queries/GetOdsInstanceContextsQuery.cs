// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;


public interface IGetOdsInstanceContextsQuery
{
    List<OdsInstanceContext> Execute();
    List<OdsInstanceContext> Execute(CommonQueryParams commonQueryParams);
}

public class GetOdsInstanceContextsQuery : IGetOdsInstanceContextsQuery
{
    private readonly IUsersContext _usersContext;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<OdsInstanceContext, object>>> _orderByColumnOds;
    public GetOdsInstanceContextsQuery(IUsersContext usersContext, IOptions<AppSettings> options)
    {
        _usersContext = usersContext;
        _options = options;
        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();
        _orderByColumnOds = new Dictionary<string, Expression<Func<OdsInstanceContext, object>>>
            (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.OdsInstanceContextKeyColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.ContextKey, DatabaseEngineEnum.SqlServerCollation) : x.ContextKey },
            { SortingColumns.OdsInstanceContextValueColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.ContextValue, DatabaseEngineEnum.SqlServerCollation) : x.ContextValue },
            { SortingColumns.DefaultIdColumn, x => x.OdsInstanceContextId }
        };
    }

    public List<OdsInstanceContext> Execute()
    {
        return _usersContext.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .OrderBy(p => p.ContextKey).ToList();
    }

    public List<OdsInstanceContext> Execute(CommonQueryParams commonQueryParams)
    {
        Expression<Func<OdsInstanceContext, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _usersContext.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}




