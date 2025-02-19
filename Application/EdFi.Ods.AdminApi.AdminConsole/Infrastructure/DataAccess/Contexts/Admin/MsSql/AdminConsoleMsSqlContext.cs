// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.ModelConfiguration;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;

public class AdminConsoleMsSqlContext : DbContext, IDbContext
{
    public AdminConsoleMsSqlContext(DbContextOptions<AdminConsoleMsSqlContext> options) : base(options) { }

    public DbSet<HealthCheck> HealthChecks { get; set; }

    public DbSet<Instance> Instances { get; set; }

    public DbSet<OdsInstanceContext> OdsInstanceContexts { get; set; }

    public DbSet<OdsInstanceDerivative> OdsInstanceDerivatives { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    public DbSet<UserProfile> UserProfiles { get; set; }

    public DbSet<Step> Steps { get; set; }

    public DatabaseFacade DB => Database;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        const string DbProvider = DbProviders.SqlServer;
        modelBuilder.ApplyConfiguration(new HealthCheckConfiguration(DbProvider));
        modelBuilder.ApplyConfiguration(new InstanceConfiguration(DbProvider));
        modelBuilder.ApplyConfiguration(new OdsInstanceDerivativeConfiguration());
        modelBuilder.ApplyConfiguration(new OdsInstanceContextConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration(DbProvider));
        modelBuilder.ApplyConfiguration(new UserProfileConfiguration(DbProvider));
        modelBuilder.ApplyConfiguration(new StepConfiguration(DbProvider));
    }
}
