// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetResourceClaimsQuery
{
    IEnumerable<ResourceClaim> Execute();
    IEnumerable<ResourceClaim> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetResourceClaimsQuery : IGetResourceClaimsQuery
{
    private readonly ISecurityContext _securityContext;
    private readonly IOptions<AppSettings> _options;
    private static readonly Dictionary<string, Expression<Func<EdFi.Security.DataAccess.Models.ResourceClaim, object>>> _orderByColumnResourceClaims =
    new Dictionary<string, Expression<Func<EdFi.Security.DataAccess.Models.ResourceClaim, object>>>
        (StringComparer.OrdinalIgnoreCase)
    {
        { SortingColumns.DefaultNameColumn, x => x.ResourceName },
        { SortingColumns.ResourceClaimParentNameColumn, x => x.ParentResourceClaim.ResourceName },
#pragma warning disable CS8603 // Possible null reference return.
        { SortingColumns.ResourceClaimParentIdColumn, x => x.ParentResourceClaimId },
#pragma warning restore CS8603 // Possible null reference return.
        { SortingColumns.DefaultIdColumn, x => x.ResourceClaimId }
    };
    public GetResourceClaimsQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    {
        _securityContext = securityContext;
        _options = options;
    }

    public IEnumerable<ResourceClaim> Execute()
    {
        return Query().ToList();
    }

    public IEnumerable<ResourceClaim> Execute(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        return Query(commonQueryParams)
            .Where(c => id == null || c.Id == id)
            .Where(c => name == null || c.Name == name)
            .ToList();
    }

    private IEnumerable<ResourceClaim> Query(CommonQueryParams? commonQueryParams = null)
    {
        Expression<Func<EdFi.Security.DataAccess.Models.ResourceClaim, object>> columnToOrderBy =
            _orderByColumnResourceClaims.GetColumnToOrderBy(commonQueryParams != null ? commonQueryParams.Value.OrderBy : string.Empty);

        var resources = new List<ResourceClaim>();

        var parentResources = _securityContext.ResourceClaims
            .Where(x => x.ParentResourceClaim == null)
            .OrderByColumn(columnToOrderBy, commonQueryParams.GetValueOrDefault().IsDescending);

        if (commonQueryParams != null)
            parentResources = parentResources.Paginate(commonQueryParams.Value.Offset, commonQueryParams.Value.Limit, _options);

        var allResources = _securityContext.ResourceClaims.ToList();

        foreach (var parentResource in parentResources.ToList())
        {
            resources.Add(BuildResourceClaimTree(parentResource, allResources));
        }

        return resources.Distinct();
    }

    private ResourceClaim BuildResourceClaimTree(EdFi.Security.DataAccess.Models.ResourceClaim resource, List<EdFi.Security.DataAccess.Models.ResourceClaim> allResources)
    {
        var children = allResources.Where(x => x.ParentResourceClaimId == resource.ResourceClaimId).ToList();

        return new ResourceClaim
        {
            Id = resource.ResourceClaimId,
            Name = resource.ResourceName,
            ParentId = resource.ParentResourceClaimId ?? 0,
            ParentName = resource.ParentResourceClaim?.ResourceName,
            Children = children.Select(child => BuildResourceClaimTree(child, allResources)).ToList()
        };
    }
}
