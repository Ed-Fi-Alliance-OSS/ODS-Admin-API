// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetResourceClaimsAsFlatListQueryBase(ISecurityContext securityContext)
{
    private readonly ISecurityContext _securityContext = securityContext;

    protected IReadOnlyList<ResourceClaim> ExecuteCore()
    {
        return _securityContext.ResourceClaims
            .Select(x => new ResourceClaim
            {
                Id = x.ResourceClaimId,
                Name = x.ResourceName,
                ParentId = x.ParentResourceClaimId ?? 0,
                ParentName = x.ParentResourceClaim.ResourceName
            })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToList();
    }
}
