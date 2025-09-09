// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.ModelConfiguration;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.PgSql;

public class AdminConsolePgSqlContext(DbContextOptions<AdminConsolePgSqlContext> options) : DbContext(options), IDbContext
{
    private IDbContextTransaction? _transaction = null;

    public IDbContextTransaction BeginTransaction()
    {
        _transaction = Database.BeginTransaction();
        return _transaction;
    }

    public void CommitTransaction()
    {
        _transaction?.Commit();
    }

    public void RollbackTransaction()
    {
        _transaction?.Rollback();
    }

    public void DisposeTransaction()
    {
        _transaction?.Dispose();
    }

    public DbSet<Instance> Instances { get; set; }

    public DbSet<OdsInstanceContext> OdsInstanceContexts { get; set; }

    public DbSet<OdsInstanceDerivative> OdsInstanceDerivatives { get; set; }

    public DatabaseFacade DB => Database;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        const string DbProvider = DbProviders.PostgreSql;
        modelBuilder.ApplyConfiguration(new InstanceConfiguration(DbProvider));
        modelBuilder.ApplyConfiguration(new OdsInstanceContextConfiguration());
        modelBuilder.ApplyConfiguration(new OdsInstanceDerivativeConfiguration());
    }
}
