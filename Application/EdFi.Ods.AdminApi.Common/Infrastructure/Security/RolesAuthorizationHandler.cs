// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using EdFi.Ods.AdminApi.Common.Settings;
using System.Linq;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security
{
    public class RolesAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>
    {
        private readonly IConfiguration _configuration;

        public RolesAuthorizationHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
        {
            var user = context.User;
            var roleClaimAttribute = _configuration.GetValue<string>("AppSettings:RoleClaimAttribute");
            var realmAccessClaim = user.FindAll(c => c.Type == roleClaimAttribute);
            if (realmAccessClaim != null && requirement?.Roles?.Count() > 0)
            {
                var roles = realmAccessClaim.Select(c => c.Value).ToList();
                foreach (var role in requirement.Roles)
                {
                    if (roles.Contains(role))
                    {
                        context.Succeed(requirement);
                        break;
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
