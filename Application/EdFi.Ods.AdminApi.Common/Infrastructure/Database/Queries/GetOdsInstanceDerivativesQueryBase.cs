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

public abstract class GetOdsInstanceDerivativesQueryBase(IUsersContext usersContext, IOptions<AppSettings> options)
{
    private readonly IUsersContext _usersContext = usersContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<OdsInstanceDerivative, object>>> _orderByColumnOds =
        CreateOrderByColumns(options);

    protected List<OdsInstanceDerivative> ExecuteCore()
    {
        return [.. _usersContext.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .OrderBy(p => p.DerivativeType)];
    }

    protected List<OdsInstanceDerivative> ExecuteCore(CommonQueryParams commonQueryParams)
    {
        Expression<Func<OdsInstanceDerivative, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return [.. _usersContext.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)];
    }

    private static Dictionary<string, Expression<Func<OdsInstanceDerivative, object>>> CreateOrderByColumns(IOptions<AppSettings> options)
    {
        var databaseEngine = options.Value.DatabaseEngine ??= DatabaseEngineEnum.SqlServer;
        var isSqlServerEngine = databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, Expression<Func<OdsInstanceDerivative, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.OdsInstanceDerivativeTypeColumn, x => isSqlServerEngine ? EF.Functions.Collate(x.DerivativeType, DatabaseEngineEnum.SqlServerCollation) : x.DerivativeType },
            { SortingColumns.OdsInstanceDerivativeOdsInstanceIdColumn, x => x.OdsInstance.OdsInstanceId },
            { SortingColumns.DefaultIdColumn, x => x.OdsInstanceDerivativeId }
        };
    }
}
