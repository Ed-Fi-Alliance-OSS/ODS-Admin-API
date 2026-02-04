// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IUsersContext = EdFi.Admin.DataAccess.Contexts.IUsersContext;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstancesQuery
{
    List<OdsInstance> Execute();

    List<OdsInstance> Execute(CommonQueryParams commonQueryParams, int? id, string? name, string? instanceType);
}

public class GetOdsInstancesQuery : IGetOdsInstancesQuery
{
    private readonly IUsersContext _usersContext;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<OdsInstance, object>>> _orderByColumnOds;

    public GetOdsInstancesQuery(IUsersContext userContext, IOptions<AppSettings> options)
    {
        _usersContext = userContext;
        _options = options;
        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();
        _orderByColumnOds = new Dictionary<string, Expression<Func<OdsInstance, object>>>
                    (StringComparer.OrdinalIgnoreCase)
                {
                    { SortingColumns.DefaultNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.Name, DatabaseEngineEnum.SqlServerCollation) : x.Name },
                    { SortingColumns.OdsInstanceInstanceTypeColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.InstanceType, DatabaseEngineEnum.SqlServerCollation) : x.InstanceType },
                    { SortingColumns.DefaultIdColumn, x => x.OdsInstanceId }
                };
    }

    public List<OdsInstance> Execute()
    {
        return _usersContext.OdsInstances.OrderBy(odsInstance => odsInstance.Name).ToList();
    }

    public List<OdsInstance> Execute(CommonQueryParams commonQueryParams, int? id, string? name, string? instanceType)
    {
        Expression<Func<OdsInstance, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _usersContext.OdsInstances
            .Where(o => id == null || o.OdsInstanceId == id)
            .Where(o => name == null || o.Name == name)
            .Where(o => instanceType == null || o.InstanceType == instanceType)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
