// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Respawn;

namespace EdFi.Ods.AdminApi.DBTestsShared;

[TestFixture]
public abstract class PlatformUsersContextTestBase
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
        Transaction(usersContext =>
        {
            foreach (var entity in entities)
            {
                ((SqlServerUsersContext)usersContext).Add(entity);
            }
        });
    }

    protected void Transaction(Action<IUsersContext> action)
    {
        using var usersContext = new SqlServerUsersContext(GetDbContextOptions());
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
