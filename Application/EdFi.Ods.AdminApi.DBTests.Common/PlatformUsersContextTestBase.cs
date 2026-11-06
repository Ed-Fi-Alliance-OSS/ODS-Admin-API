// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;

namespace EdFi.Ods.AdminApi.DBTestsShared;

[TestFixture]
public abstract class PlatformUsersContextTestBase
{
    private Respawner _respawner;

    protected abstract string AdminConnectionString { get; }

    protected string ConnectionString => AdminConnectionString;

    private async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                TablesToIgnore =
                [
                    new Table("__MigrationHistory"),
                    new Table("DeployJournal"),
                    new Table("AdminApiDeployJournal")
                ]
            });

        await _respawner.ResetAsync(connection);
    }
    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        await ResetDatabaseAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await ResetDatabaseAsync();
    }

    protected void Save(params object[] entities)
    {
        Transaction(usersContext =>
        {
            foreach (var entity in entities)
            {
                ((AdminApiSqlServerUsersContext)usersContext).Add(entity);
            }
        });
    }

    protected void Transaction(Action<IUsersContext> action)
    {
        using var usersContext = new AdminApiSqlServerUsersContext(GetDbContextOptions());
        using var transaction = (usersContext).Database.BeginTransaction();
        action(usersContext);
        usersContext.SaveChanges();
        transaction.Commit();
    }

    protected TResult Transaction<TResult>(Func<IUsersContext, TResult> query)
    {
        var result = default(TResult);

        Transaction(database =>
        {
            result = query(database);
        });

        return result;
    }

    protected DbContextOptions GetDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder();
        builder.UseSqlServer(ConnectionString);
        return builder.Options;
    }
}
