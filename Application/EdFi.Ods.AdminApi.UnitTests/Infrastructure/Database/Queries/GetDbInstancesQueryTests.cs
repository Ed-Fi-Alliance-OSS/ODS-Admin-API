// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDbInstancesQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetDbInstancesQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_WithoutFilters_ReturnsAllDbInstances()
    {
        using var context = CreateContext();
        context.DbInstances.AddRange(
            new DbInstance { Name = "Sandbox A", Status = "Healthy", DatabaseTemplate = "Minimal" },
            new DbInstance { Name = "Sandbox B", Status = "Healthy", DatabaseTemplate = "Minimal" });
        context.SaveChanges();

        var query = new GetDbInstancesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, null);

        result.Count.ShouldBe(2);
        result.Select(x => x.Name).ShouldBe(["Sandbox A", "Sandbox B"], ignoreOrder: true);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatchingDbInstance()
    {
        using var context = CreateContext();
        context.DbInstances.AddRange(
            new DbInstance { Name = "Sandbox A", Status = "Healthy", DatabaseTemplate = "Minimal" },
            new DbInstance { Name = "Sandbox B", Status = "Healthy", DatabaseTemplate = "Minimal" });
        context.SaveChanges();

        var query = new GetDbInstancesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, "Sandbox B");

        result.Count.ShouldBe(1);
        result.Single().Name.ShouldBe("Sandbox B");
    }
}
