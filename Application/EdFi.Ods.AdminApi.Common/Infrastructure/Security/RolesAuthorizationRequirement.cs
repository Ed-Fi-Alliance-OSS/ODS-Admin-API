// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Authorization;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security
{
    public class RolesAuthorizationRequirement : IAuthorizationRequirement
    {
        public IEnumerable<string> Roles { get; }

        public RolesAuthorizationRequirement(IEnumerable<string> roles)
        {
            Roles = roles;
        }
    }
}
