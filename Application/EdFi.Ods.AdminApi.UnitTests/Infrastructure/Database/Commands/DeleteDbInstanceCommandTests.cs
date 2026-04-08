// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class DeleteDbInstanceCommandTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteDbInstanceCommand_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["AppSettings:DatabaseEngine"] = "SqlServer" }
            )
            .Build();
        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_SetsStatusToPendingDelete()
    {
        using var context = CreateContext();
        var instance = new DbInstance
        {
            Name = "Test Instance",
            Status = DbInstanceStatus.Pending.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow,
        };
        context.DbInstances.Add(instance);
        context.SaveChanges();

        var command = new DeleteDbInstanceCommand(context);
        command.Execute(instance.Id);

        var updated = context.DbInstances.Single(d => d.Id == instance.Id);
        updated.Status.ShouldBe(DbInstanceStatus.PendingDelete.ToString());
    }

    [Test]
    public void Execute_UpdatesLastModifiedDate()
    {
        using var context = CreateContext();
        var before = DateTime.UtcNow;
        var instance = new DbInstance
        {
            Name = "Test Instance",
            Status = DbInstanceStatus.Pending.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow,
        };
        context.DbInstances.Add(instance);
        context.SaveChanges();

        var command = new DeleteDbInstanceCommand(context);
        command.Execute(instance.Id);

        var updated = context.DbInstances.Single(d => d.Id == instance.Id);
        updated.LastModifiedDate.ShouldNotBeNull();
        updated.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public void Execute_WithNonExistentId_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var command = new DeleteDbInstanceCommand(context);

        Should.Throw<ArgumentException>(() => command.Execute(9999));
    }
}

#nullable restore
