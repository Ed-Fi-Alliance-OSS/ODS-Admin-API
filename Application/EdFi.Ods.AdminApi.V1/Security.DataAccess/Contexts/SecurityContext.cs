// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Action = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts
{
    public abstract class SecurityContext : DbContext, ISecurityContext
    {
        protected SecurityContext(DbContextOptions options)
            : base(options) { }

        public DbSet<Application> Applications { get; set; }

        public DbSet<Action> Actions { get; set; }

        public DbSet<AuthorizationStrategy> AuthorizationStrategies { get; set; }

        public DbSet<ClaimSet> ClaimSets { get; set; }

        public DbSet<ClaimSetResourceClaimAction> ClaimSetResourceClaimActions { get; set; }

        public DbSet<ResourceClaim> ResourceClaims { get; set; }

        public DbSet<ResourceClaimAction> ResourceClaimActions { get; set; }

        public DbSet<ClaimSetResourceClaimActionAuthorizationStrategyOverrides> ClaimSetResourceClaimActionAuthorizationStrategyOverrides { get; set; }

        public DbSet<ResourceClaimActionAuthorizationStrategies> ResourceClaimActionAuthorizationStrategies { get; set; }

    }
}
