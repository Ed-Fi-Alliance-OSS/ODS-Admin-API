// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using static EdFi.Ods.AdminConsole.DBTests.Testing;

namespace EdFi.Ods.AdminConsole.DBTests;

[TestFixture]
public abstract class PlatformUsersContextTestBase
{

    protected static string ConnectionString => AdminConnectionString;

    protected static void Save(params object[] entities)
    {
        Transaction(dbContext =>
        {
            foreach (var entity in entities)
            {
                ((AdminConsoleMsSqlContext)dbContext).Add(entity);
            }
        });
    }

    protected static void Transaction(Action<IDbContext> action)
    {
        using var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());
        using var transaction = (dbContext).Database.BeginTransaction();
        action(dbContext);
        dbContext.SaveChanges();
        transaction.Commit();
    }

    protected static async Task TransactionAsync(Func<IDbContext, Task> action)
    {
        using var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());
        using var transaction = await dbContext.Database.BeginTransactionAsync();
        await action(dbContext);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    protected static TResult Transaction<TResult>(Func<IDbContext, TResult> query)
    {
        var result = default(TResult);

        Transaction(database =>
        {
            result = query(database);
        });

        return result;
    }

    protected static DbContextOptions<AdminConsoleMsSqlContext> GetDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder<AdminConsoleMsSqlContext>();
        builder.UseSqlServer(ConnectionString);
        return builder.Options;
    }

    protected static DbContextOptions<AdminConsoleSqlServerUsersContext> GetUserDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>();
        builder.UseSqlServer(ConnectionString);
        return builder.Options;
    }
}
