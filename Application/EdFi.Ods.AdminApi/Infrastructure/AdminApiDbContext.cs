// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure;

public class AdminApiDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AdminApiDbContext(DbContextOptions<AdminApiDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("adminapi");

        modelBuilder.Entity<ApiApplication>().ToTable("Applications").HasKey(a => a.Id);
        modelBuilder.Entity<ApiScope>().ToTable("Scopes").HasKey(s => s.Id);
        modelBuilder.Entity<ApiAuthorization>().ToTable("Authorizations").HasKey(a => a.Id);
        modelBuilder.Entity<ApiToken>().ToTable("Tokens").HasKey(t => t.Id);

        var engine = _configuration.Get("AppSettings:DatabaseEngine", "SqlServer");
        modelBuilder.ApplyDatabaseServerSpecificConventions(engine);
    }
}
