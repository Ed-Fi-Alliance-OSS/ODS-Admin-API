// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Respawn;

namespace EdFi.Ods.AdminApi.DBTests;

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

    protected static string ConnectionString => Testing.AdminConnectionString;

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

    protected static void Save(params object[] entities)
    {
        Transaction(context =>
        {
            foreach (var entity in entities)
            {
                context.Add(entity);
            }
        });
    }

    protected static void Transaction(System.Action<AdminApiDbContext> action)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Testing.Configuration());
        using var transaction = context.Database.BeginTransaction();
        action(context);
        context.SaveChanges();
        transaction.Commit();
    }

    protected static TResult Transaction<TResult>(System.Func<AdminApiDbContext, TResult> query)
    {
        var result = default(TResult);
        Transaction(database =>
        {
            result = query(database);
        });
        return result;
    }

    public static DbContextOptions<AdminApiDbContext> GetAdminApiDbContextOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AdminApiDbContext>();
        builder.UseSqlServer(connectionString);
        return builder.Options;
    }
}
