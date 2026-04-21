// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Action = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetAllActionsQueryBase(ISecurityContext securityContext, IOptions<AppSettings> options)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<Action, object>>> _orderByColumnActions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.ActionName, DatabaseEngineEnum.SqlServerCollation) : x.ActionName },
            { SortingColumns.ActionUriColumn, x => x.ActionUri },
            { SortingColumns.DefaultIdColumn, x => x.ActionId }
        };

    protected IReadOnlyList<Action> ExecuteCore()
    {
        return _securityContext.Actions.ToList();
    }

    protected IReadOnlyList<Action> ExecuteCore(CommonQueryParams commonQueryParams, int? id, string? name)
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
