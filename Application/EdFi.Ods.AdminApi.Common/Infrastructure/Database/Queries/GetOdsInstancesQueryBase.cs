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

public abstract class GetOdsInstancesQueryBase(IUsersContext usersContext, IOptions<AppSettings> options)
{
    private readonly IUsersContext _usersContext = usersContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<OdsInstance, object>>> _orderByColumnOds =
        CreateOrderByColumns(options);

    protected List<OdsInstance> ExecuteCore()
    {
        return [.. _usersContext.OdsInstances.OrderBy(odsInstance => odsInstance.Name)];
    }

    protected List<OdsInstance> ExecuteCore(CommonQueryParams commonQueryParams, int? id, string? name, string? instanceType)
    {
        Expression<Func<OdsInstance, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return [.. _usersContext.OdsInstances
            .Where(o => id == null || o.OdsInstanceId == id)
            .Where(o => name == null || o.Name == name)
            .Where(o => instanceType == null || o.InstanceType == instanceType)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)];
    }

    private static Dictionary<string, Expression<Func<OdsInstance, object>>> CreateOrderByColumns(IOptions<AppSettings> options)
    {
        var databaseEngine = options.Value.DatabaseEngine ??= DatabaseEngineEnum.SqlServer;
        var isSqlServerEngine = databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, Expression<Func<OdsInstance, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultNameColumn, x => isSqlServerEngine ? EF.Functions.Collate(x.Name, DatabaseEngineEnum.SqlServerCollation) : x.Name },
            { SortingColumns.OdsInstanceInstanceTypeColumn, x => isSqlServerEngine ? EF.Functions.Collate(x.InstanceType, DatabaseEngineEnum.SqlServerCollation) : x.InstanceType },
            { SortingColumns.DefaultIdColumn, x => x.OdsInstanceId }
        };
    }
}
