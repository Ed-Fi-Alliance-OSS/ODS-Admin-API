// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetAllClaimSetsQueryBase(ISecurityContext securityContext, IOptions<AppSettings> options)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<ClaimSet, object>>> _orderByColumnClaimSet =
        new(StringComparer.OrdinalIgnoreCase)
        {
#pragma warning disable CS8603
            { SortingColumns.DefaultNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.Name, DatabaseEngineEnum.SqlServerCollation) : x.Name },
#pragma warning restore CS8603
            { SortingColumns.DefaultIdColumn, x => x.Id }
        };

    protected IReadOnlyList<ClaimSet> ExecuteCore()
    {
        return _securityContext.ClaimSets
            .Select(x => new ClaimSet
            {
                Id = x.ClaimSetId,
                Name = x.ClaimSetName,
                IsEditable = !x.IsEdfiPreset && !x.ForApplicationUseOnly
            })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToList();
    }

    protected IReadOnlyList<ClaimSet> ExecuteCore(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        Expression<Func<ClaimSet, object>> columnToOrderBy = _orderByColumnClaimSet.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _securityContext.ClaimSets
            .Where(c => id == null || c.ClaimSetId == id)
            .Where(c => name == null || c.ClaimSetName == name)
            .Select(x => new ClaimSet
            {
                Id = x.ClaimSetId,
                Name = x.ClaimSetName,
                IsEditable = !x.IsEdfiPreset && !x.ForApplicationUseOnly
            })
            .Distinct()
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
