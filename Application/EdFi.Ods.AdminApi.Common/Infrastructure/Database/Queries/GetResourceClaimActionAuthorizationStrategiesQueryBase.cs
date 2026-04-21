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
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetResourceClaimActionAuthorizationStrategiesQueryBase(ISecurityContext securityContext, IOptions<AppSettings> options)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<ResourceClaimActionAuthStrategyModel, object>>> _orderByColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultIdColumn, x => x.ResourceClaimId },
            { nameof(ResourceClaimActionAuthStrategyModel.ResourceName), x => x.ResourceName },
            { nameof(ResourceClaimActionAuthStrategyModel.ClaimName), x => x.ClaimName }
        };

    protected IReadOnlyList<ResourceClaimActionAuthStrategyModel> ExecuteCore(CommonQueryParams commonQueryParams, string? resourceName)
    {
        Expression<Func<ResourceClaimActionAuthStrategyModel, object>> columnToOrderBy = _orderByColumns.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _securityContext.ResourceClaimActionAuthorizationStrategies
            .Where(w => resourceName == null || w.ResourceClaimAction.ResourceClaim.ResourceName == resourceName)
            .GroupBy(gb => new
            {
                gb.ResourceClaimAction.ResourceClaimId,
                gb.ResourceClaimAction.ResourceClaim.ResourceName,
                gb.ResourceClaimAction.ResourceClaim.ClaimName
            })
            .Select(group => new ResourceClaimActionAuthStrategyModel
            {
                ResourceClaimId = group.Key.ResourceClaimId,
                ResourceName = group.Key.ResourceName,
                ClaimName = group.Key.ClaimName,
                AuthorizationStrategiesForActions = group.GroupBy(gb => new
                    {
                        gb.ResourceClaimAction.Action.ActionId,
                        gb.ResourceClaimAction.Action.ActionName
                    })
                    .Select(groupedActions => new ActionWithAuthorizationStrategy
                    {
                        ActionId = groupedActions.Key.ActionId,
                        ActionName = groupedActions.Key.ActionName,
                        AuthorizationStrategies = groupedActions.Select(resourceClaimActionAuthorizationStrategies =>
                            new AuthorizationStrategyModelForAction
                            {
                                AuthStrategyId = resourceClaimActionAuthorizationStrategies.AuthorizationStrategy.AuthorizationStrategyId,
                                AuthStrategyName = resourceClaimActionAuthorizationStrategies.AuthorizationStrategy.AuthorizationStrategyName,
                            }).ToList()
                    }).ToList()
            })
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
