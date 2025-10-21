// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public class GetChildResourceClaimsForParentQuery
{
    private readonly ISecurityContext _securityContext;

    public GetChildResourceClaimsForParentQuery(ISecurityContext securityContext)
    => _securityContext = securityContext;

    public IEnumerable<ResourceClaim> Execute(int parentResourceClaimId)
    {
        var parentResourceClaim = _securityContext.ResourceClaims.SingleOrDefault(
                rc => rc.ResourceClaimId == parentResourceClaimId);

        var childResourcesForParent = _securityContext.ResourceClaims
            .Where(x => x.ParentResourceClaimId == parentResourceClaimId).ToList();

        return [.. childResourcesForParent
            .Select(x => new ResourceClaim()
            {
                Name = x.ResourceName,
                Id = x.ResourceClaimId,
                ParentId = parentResourceClaimId,
                ParentName = parentResourceClaim?.ResourceName ?? string.Empty,
            })
            .Distinct()
            .OrderBy(x => x.Name)];
    }
}
