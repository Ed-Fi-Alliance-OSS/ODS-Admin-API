// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetResourceClaimByResourceClaimIdQueryBase(ISecurityContext securityContext)
{
    private readonly ISecurityContext _securityContext = securityContext;

    protected ResourceClaim ExecuteCore(int id)
    {
        var resource = _securityContext.ResourceClaims.FirstOrDefault(x => x.ResourceClaimId == id);
        if (resource == null)
        {
            throw new NotFoundException<int>("resourceclaim", id);
        }

        var children = _securityContext.ResourceClaims.Where(x => x.ParentResourceClaimId == resource.ResourceClaimId);
        return new ResourceClaim
        {
            Children = children.Select(child => new ResourceClaim
            {
                Id = child.ResourceClaimId,
                Name = child.ResourceName,
                ParentId = resource.ResourceClaimId,
                ParentName = resource.ResourceName,
            }).ToList(),
            Name = resource.ResourceName,
            Id = resource.ResourceClaimId
        };
    }
}
