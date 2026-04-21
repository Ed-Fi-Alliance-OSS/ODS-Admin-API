// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using CommonResourceClaim = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaim;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public class GetChildResourceClaimsForParentQuery(ISecurityContext securityContext)
    : GetChildResourceClaimsForParentQueryBase(securityContext)
{
    public IEnumerable<ResourceClaim> Execute(int parentResourceClaimId)
    {
        return ExecuteCore(parentResourceClaimId)
            .Select(Map)
            .ToList();
    }

    private static ResourceClaim Map(CommonResourceClaim resourceClaim)
    {
        return new ResourceClaim
        {
            Id = resourceClaim.Id,
            Name = resourceClaim.Name,
            ParentId = resourceClaim.ParentId,
            ParentName = resourceClaim.ParentName,
            Children = resourceClaim.Children.Select(Map).ToList()
        };
    }
}


