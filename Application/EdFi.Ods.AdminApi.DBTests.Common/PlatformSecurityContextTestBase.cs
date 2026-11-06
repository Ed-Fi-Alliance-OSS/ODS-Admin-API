// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;

namespace EdFi.Ods.AdminApi.DBTestsShared;

[TestFixture]
public abstract class PlatformSecurityContextTestBase
{
    protected SqlServerSecurityContext TestContext { get; private set; }

    protected enum CheckpointPolicyOptions
    {
        BeforeEachTest,
        BeforeAnyTest
    }

    protected CheckpointPolicyOptions CheckpointPolicy { get; set; } = CheckpointPolicyOptions.BeforeEachTest;

    private Respawner _respawner;

    protected virtual string ConnectionString => TestContext.Database.GetConnectionString();

    protected abstract string AdminConnectionString { get; }

    protected virtual void AdditionalFixtureSetup()
    {
    }

    protected abstract SqlServerSecurityContext CreateDbContext();

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
    [OneTimeSetUp]
    public virtual async Task FixtureSetup()
    {
        TestContext = CreateDbContext();

        if (CheckpointPolicy == CheckpointPolicyOptions.BeforeAnyTest)
        {
            await ResetDatabaseAsync();
        }

        AdditionalFixtureSetup();
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        await ResetDatabaseAsync();
        if (TestContext != null)
        {
            TestContext.Dispose();
            TestContext = null;
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        TestContext = CreateDbContext();

        if (CheckpointPolicy == CheckpointPolicyOptions.BeforeEachTest)
        {
            await ResetDatabaseAsync();
        }
    }

    [TearDown]
    public void TearDown()
    {
        TestContext.Dispose();
    }

    protected void Save(params object[] entities)
    {
        foreach (var entity in entities)
        {
            TestContext.Add(entity);
        }

        TestContext.SaveChanges();
    }

    protected void UsersTransaction(Action<IUsersContext> action)
    {
        var options = new DbContextOptionsBuilder().UseSqlServer(AdminConnectionString).Options;
        using var usersContext = new SqlServerUsersContext(options);
        using var transaction = usersContext.Database.BeginTransaction();
        action(usersContext);
        usersContext.SaveChanges();
        transaction.Commit();
    }

    protected TResult UsersTransaction<TResult>(Func<IUsersContext, TResult> query)
    {
        var result = default(TResult);

        UsersTransaction(database =>
        {
            result = query(database);
        });

        return result;
    }

    protected void Transaction(Action<ISecurityContext> action)
    {
        using var transaction = TestContext.Database.BeginTransaction();
        action(TestContext);
        TestContext.SaveChanges();
        transaction.Commit();
    }

    protected TResult Transaction<TResult>(Func<ISecurityContext, TResult> query)
    {
        var result = default(TResult);

        Transaction(database =>
        {
            result = query(database);
        });

        return result;
    }
}
