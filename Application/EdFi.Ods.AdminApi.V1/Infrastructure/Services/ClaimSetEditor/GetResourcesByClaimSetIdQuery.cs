// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor
{
    public class GetResourcesByClaimSetIdQuery(GetResourcesByClaimSetIdQueryService service) : IGetResourcesByClaimSetIdQuery
    {
        private readonly GetResourcesByClaimSetIdQueryService _service = service;

        public IList<ResourceClaim> AllResources(int securityContextClaimSetId)
        {
            IList<ResourceClaim> parentResources;

            return ModelSix();

            IList<ResourceClaim> ModelSix()
            {
                parentResources = _service.GetParentResources(securityContextClaimSetId);
                var childResources = _service.GetChildResources(securityContextClaimSetId);
                GetResourcesByClaimSetIdQueryService.AddChildResourcesToParents(childResources, parentResources);

                return parentResources;
            }
        }

        public ResourceClaim? SingleResource(int claimSetId, int resourceClaimId)
        {
            var parentResources = AllResources(claimSetId).ToList();
            var parentResourceClaim = parentResources
                .SingleOrDefault(x => x.Id == resourceClaimId);
            var childResources = new List<ResourceClaim>();
            if (parentResourceClaim == null)
            {
                foreach (var resourceClaims in parentResources.Select(x => x.Children))
                    childResources.AddRange(resourceClaims);
                return childResources.SingleOrDefault(x => x.Id == resourceClaimId);
            }

            return parentResourceClaim;
        }
    }

    public interface IGetResourcesByClaimSetIdQuery
    {
        IList<ResourceClaim> AllResources(int securityContextClaimSetId);
        ResourceClaim? SingleResource(int claimSetId, int resourceClaimId);
    }
}
