// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EdFi.Ods.AdminApi.Infrastructure;

public class AdminConsolePostgresUsersContext(DbContextOptions options) : PostgresUsersContext(options), IAdminApiUserContext
{
    public DbSet<EducationOrganization> EducationOrganizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EducationOrganization>().ToTable("EducationOrganizations", "adminapi").HasKey(t => t.Id);
    }

    public void UseTransaction(IDbContextTransaction transaction)
    {
        Database.UseTransaction(transaction.GetDbTransaction());
    }
}
