// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceContextsQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceContextsQueryTests_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsAllContextsOrderedByContextKey()
    {
        using var context = CreateContext();
        context.OdsInstanceContexts.AddRange(
            new OdsInstanceContext
            {
                OdsInstance = new OdsInstance { Name = "Instance One", InstanceType = "type", ConnectionString = "cs1" },
                ContextKey = "zeta",
                ContextValue = "1"
            },
            new OdsInstanceContext
            {
                OdsInstance = new OdsInstance { Name = "Instance Two", InstanceType = "type", ConnectionString = "cs2" },
                ContextKey = "alpha",
                ContextValue = "2"
            });
        context.SaveChanges();

        var query = new GetOdsInstanceContextsQuery(context, DefaultOptions());

        var result = query.Execute();

        result.Select(x => x.ContextKey).ShouldBe(["alpha", "zeta"]);
    }

    [Test]
    public void Execute_WithCommonQueryParams_AppliesPagination()
    {
        using var context = CreateContext();
        context.OdsInstanceContexts.AddRange(
            new OdsInstanceContext
            {
                OdsInstance = new OdsInstance { Name = "Instance One", InstanceType = "type", ConnectionString = "cs1" },
                ContextKey = "alpha",
                ContextValue = "1"
            },
            new OdsInstanceContext
            {
                OdsInstance = new OdsInstance { Name = "Instance Two", InstanceType = "type", ConnectionString = "cs2" },
                ContextKey = "beta",
                ContextValue = "2"
            });
        context.SaveChanges();

        var query = new GetOdsInstanceContextsQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 1));

        result.Count.ShouldBe(1);
        result.Single().ContextKey.ShouldBe("alpha");
    }
}
