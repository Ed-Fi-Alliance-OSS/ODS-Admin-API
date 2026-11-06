// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database;

public class AdminApiSqlServerUsersContext : SqlServerUsersContext
{
    public AdminApiSqlServerUsersContext(DbContextOptions options)
        : base(options)
    {
        ChangeTracker.Tracked += ApiClientShadowProperties.DefaultNewApiClientsToInheriting;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApiClientShadowProperties.Configure(modelBuilder, "InheritsApplicationEducationOrganizations");
    }
}
