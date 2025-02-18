// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.ResourceClaimActionAuthStrategies;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetResourceClaimActionAuthorizationStrategiesQuery
{
    public IReadOnlyList<ResourceClaimActionAuthStrategyModel> Execute(CommonQueryParams commonQueryParams, string? resourceName);
}

public class GetResourceClaimActionAuthorizationStrategiesQuery : IGetResourceClaimActionAuthorizationStrategiesQuery
{
    private readonly ISecurityContext _securityContext;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<ResourceClaimActionAuthStrategyModel, object>>> _orderByColumns;

    public GetResourceClaimActionAuthorizationStrategiesQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    {
        _securityContext = securityContext;
        _options = options;
        _orderByColumns = new Dictionary<string, Expression<Func<ResourceClaimActionAuthStrategyModel, object>>>
            (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.DefaultIdColumn, x => x.ResourceClaimId },
            { nameof(ResourceClaimActionAuthStrategyModel.ResourceName), x => x.ResourceName },
            { nameof(ResourceClaimActionAuthStrategyModel.ClaimName), x => x.ClaimName }
        };
    }

    public IReadOnlyList<ResourceClaimActionAuthStrategyModel> Execute(CommonQueryParams commonQueryParams, string? resourceName)
    {
        Expression<Func<ResourceClaimActionAuthStrategyModel, object>> columnToOrderBy = _orderByColumns.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _securityContext.ResourceClaimActionAuthorizationStrategies
            // Group by ResourceClaimId and ResourceName to structure the JSON correctly
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
                // Group by ActionId and ActionName to create a list of actions within the resource
                AuthorizationStrategiesForActions = group.GroupBy(gb => new
                {
                    gb.ResourceClaimAction.Action.ActionId,
                    gb.ResourceClaimAction.Action.ActionName
                })
                .Select(groupedActions => new ActionWithAuthorizationStrategy
                {
                    ActionId = groupedActions.Key.ActionId,
                    ActionName = groupedActions.Key.ActionName,
                    // For each action, get the associated authorization strategies
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
