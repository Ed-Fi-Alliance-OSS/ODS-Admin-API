// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public interface IGetResourceClaimsQuery
{
    IEnumerable<ResourceClaim> Execute();
}

public class GetResourceClaimsQuery : IGetResourceClaimsQuery
{
    private readonly ISecurityContext _securityContext;

    public GetResourceClaimsQuery(ISecurityContext securityContext)
    => _securityContext = securityContext;

    public IEnumerable<ResourceClaim> Execute()
    {
        var resources = new List<ResourceClaim>();
        var parentResources = _securityContext.ResourceClaims.Where(x => x.ParentResourceClaim == null).ToList();
        var childResources = _securityContext.ResourceClaims.Where(x => x.ParentResourceClaim != null).ToList();
        foreach (var parentResource in parentResources)
        {
            var children = childResources.Where(x => x.ParentResourceClaimId == parentResource.ResourceClaimId);
            resources.Add(new ResourceClaim
            {
                Children = children.Select(child => new ResourceClaim()
                {
                    Id = child.ResourceClaimId,
                    Name = child.ResourceName,
                    ParentId = parentResource.ResourceClaimId,
                    ParentName = parentResource.ResourceName,
                }).ToList(),
                Name = parentResource.ResourceName,
                Id = parentResource.ResourceClaimId
            });
        }
        return [.. resources
            .Distinct()
            .OrderBy(x => x.Name)];
    }
}
