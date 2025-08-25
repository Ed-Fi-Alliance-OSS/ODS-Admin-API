// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using Action = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Action;

namespace EdFi.Security.DataAccess.V1.Caching
{
    public class InstanceSecurityRepositoryCacheObject
    {
        public Application Application { get; }

        public List<Action> Actions { get; }

        public List<ClaimSet> ClaimSets { get; }

        public List<ResourceClaim> ResourceClaims { get; }

        public List<AuthorizationStrategy> AuthorizationStrategies { get; }

        public List<ClaimSetResourceClaimAction> ClaimSetResourceClaimActions { get; }

        public List<ResourceClaimAction> ResourceClaimActions { get; }


        public InstanceSecurityRepositoryCacheObject(
            Application application,
            List<Action> actions,
            List<ClaimSet> claimSets,
            List<ResourceClaim> resourceClaims,
            List<AuthorizationStrategy> authorizationStrategies,
            List<ClaimSetResourceClaimAction> claimSetResourceClaimActions,
            List<ResourceClaimAction> resourceClaimActions)
        {
            Application = application;
            Actions = actions;
            ClaimSets = claimSets;
            ResourceClaims = resourceClaims;
            AuthorizationStrategies = authorizationStrategies;
            ClaimSetResourceClaimActions = claimSetResourceClaimActions;
            ResourceClaimActions = resourceClaimActions;
        }
    }
}
