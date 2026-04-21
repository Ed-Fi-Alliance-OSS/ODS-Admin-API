// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetResourceClaimActionsQueryBase(ISecurityContext securityContext, IOptions<AppSettings> options)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<ResourceClaimActionModel, object>>> _orderByColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(ResourceClaimActionModel.ResourceClaimId), x => x.ResourceClaimId },
            { nameof(ResourceClaimActionModel.ResourceName), x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.ResourceName, DatabaseEngineEnum.SqlServerCollation) : x.ResourceName },
        };

    protected IReadOnlyList<ResourceClaimActionModel> ExecuteCore(CommonQueryParams commonQueryParams, string? resourceName)
    {
        Expression<Func<ResourceClaimActionModel, object>> columnToOrderBy = _orderByColumns.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _securityContext.ResourceClaimActions
            .Include(i => i.ResourceClaim)
            .Include(i => i.Action)
            .Where(w => resourceName == null || w.ResourceClaim.ResourceName == resourceName)
            .GroupBy(r => new { r.ResourceClaim.ResourceClaimId, r.ResourceClaim.ResourceName, r.ResourceClaim.ClaimName })
            .Select(group => new ResourceClaimActionModel
            {
                ResourceClaimId = group.Key.ResourceClaimId,
                ResourceName = group.Key.ResourceName,
                ClaimName = group.Key.ClaimName,
                Actions = group.Select(g => new ActionForResourceClaimModel { Name = g.Action.ActionName }).Distinct().ToList()
            })
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
