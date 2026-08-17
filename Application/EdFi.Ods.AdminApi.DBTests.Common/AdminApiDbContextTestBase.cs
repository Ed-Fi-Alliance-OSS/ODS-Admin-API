// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Respawn;

namespace EdFi.Ods.AdminApi.DBTestsShared;

[TestFixture]
public abstract class AdminApiDbContextTestBase
{
    private readonly Checkpoint _checkpoint = new()
    {
        TablesToIgnore =
        [
            "__MigrationHistory", "DeployJournal", "AdminApiDeployJournal"
        ],
        SchemasToExclude = []
    };

    protected abstract string AdminConnectionString { get; }

    protected abstract IConfiguration Configuration { get; }

    protected string ConnectionString => AdminConnectionString;

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        await _checkpoint.Reset(ConnectionString);
    }

    [SetUp]
    public async Task SetUp()
    {
        await _checkpoint.Reset(ConnectionString);
    }

    protected void Save(params object[] entities)
    {
        Transaction(context =>
        {
            foreach (var entity in entities)
            {
                context.Add(entity);
            }
        });
    }

    protected void Transaction(System.Action<AdminApiDbContext> action)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Configuration);
        using var transaction = context.Database.BeginTransaction();
        action(context);
        context.SaveChanges();
        transaction.Commit();
    }

    protected TResult Transaction<TResult>(System.Func<AdminApiDbContext, TResult> query)
    {
        var result = default(TResult);
        Transaction(database =>
        {
            result = query(database);
        });
        return result;
    }

    protected async Task Transaction(System.Func<AdminApiDbContext, Task> action)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Configuration);
        using var transaction = await context.Database.BeginTransactionAsync();
        await action(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    protected async Task<TResult> Transaction<TResult>(System.Func<AdminApiDbContext, Task<TResult>> query)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Configuration);
        using var transaction = await context.Database.BeginTransactionAsync();
        var result = await query(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    protected DbContextOptions<AdminApiDbContext> GetAdminApiDbContextOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AdminApiDbContext>();
        builder.UseSqlServer(connectionString);
        return builder.Options;
    }
}
