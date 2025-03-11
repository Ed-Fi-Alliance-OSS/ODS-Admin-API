// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security
{
    public static class Roles
    {
        public static readonly RoleDefinition AdminApiClientRole
            = new RoleDefinition("adminapi-client", "AminApi Client");
        public static readonly RoleDefinition AdminConsoleUserRole
            = new RoleDefinition("adminconsole-user", "AdminConsole User");
        public static readonly IEnumerable<RoleDefinition> AllRoles
            = new List<RoleDefinition> { AdminApiClientRole, AdminConsoleUserRole };
    }
    public class RoleDefinition
    {
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public RoleDefinition(string roleName, string description)
        {
            RoleName = roleName;
            RoleDescription = description;
        }
    }
}
