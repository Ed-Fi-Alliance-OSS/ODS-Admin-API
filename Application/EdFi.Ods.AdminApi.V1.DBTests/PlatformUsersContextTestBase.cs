// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;
using static EdFi.Ods.AdminApi.V1.DBTests.Testing;

namespace EdFi.Ods.AdminApi.V1.DBTests;

[TestFixture]
public abstract class PlatformUsersContextTestBase
{
    private Respawner _respawner;

    protected static string ConnectionString => AdminConnectionString;

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

    protected static void Save(params object[] entities)
    {
        Transaction(usersContext =>
        {
            foreach (var entity in entities)
            {
                ((SqlServerUsersContext)usersContext).Add(entity);
            }
        });
    }

    protected static void Transaction(Action<IUsersContext> action)
    {
        using var usersContext = new SqlServerUsersContext(GetDbContextOptions());
        using var transaction = (usersContext).Database.BeginTransaction();
        action(usersContext);
        usersContext.SaveChanges();
        transaction.Commit();
    }

    protected static TResult Transaction<TResult>(Func<IUsersContext, TResult> query)
    {
        var result = default(TResult);

        Transaction(database =>
        {
            result = query(database);
        });

        return result;
    }

    protected static DbContextOptions GetDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder();
        builder.UseSqlServer(ConnectionString);
        return builder.Options;
    }
}
