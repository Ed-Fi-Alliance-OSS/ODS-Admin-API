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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Action = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetAllActionsQuery
{
    IReadOnlyList<Action> Execute();
    IReadOnlyList<Action> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetAllActionsQuery : IGetAllActionsQuery
{
    private readonly ISecurityContext _securityContext;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<Action, object>>> _orderByColumnActions;

    public GetAllActionsQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    {
        _securityContext = securityContext;
        _options = options;
        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();
        _orderByColumnActions = new Dictionary<string, Expression<Func<Action, object>>>
        (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.ActionName, DatabaseEngineEnum.SqlServerCollation) : x.ActionName },
            { SortingColumns.ActionUriColumn, x => x.ActionUri },
            { SortingColumns.DefaultIdColumn, x => x.ActionId }
        };
    }

    public IReadOnlyList<Action> Execute()
    {
        return _securityContext.Actions.ToList();
    }

    public IReadOnlyList<Action> Execute(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        Expression<Func<Action, object>> columnToOrderBy = _orderByColumnActions.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _securityContext.Actions
        .Where(a => id == null || a.ActionId == id)
        .Where(a => name == null || a.ActionName == name)
        .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
        .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
        .ToList();
    }
}
