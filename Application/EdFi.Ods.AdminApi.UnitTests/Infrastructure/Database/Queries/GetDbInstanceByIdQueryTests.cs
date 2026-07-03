// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDbInstanceByIdQueryTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetDbInstanceByIdQueryTests_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "Postgres"
            })
            .Build();

        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_WithExistingId_ReturnsDbInstance()
    {
        using var context = CreateContext();
        var dbInstance = new DbInstance
        {
            Name = "Sandbox",
            Status = "Healthy",
            DatabaseTemplate = "Minimal"
        };
        context.DbInstances.Add(dbInstance);
        context.SaveChanges();

        var query = new GetDbInstanceByIdQuery(context);

        var result = query.Execute(dbInstance.Id);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Sandbox");
    }

    [Test]
    public void Execute_WithUnknownId_ReturnsNull()
    {
        using var context = CreateContext();
        var query = new GetDbInstanceByIdQuery(context);

        var result = query.Execute(999);

        result.ShouldBeNull();
    }
}
