// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.V3.Infrastructure.Database.Commands;

[TestFixture]
public class AddDbInstanceCommandTests
{
    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"AddDbInstanceCommand_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "SqlServer"
            })
            .Build();
        return new AdminApiDbContext(options, configuration);
    }

    [Test]
    public void Execute_WithValidModel_PersistsDbInstance()
    {
        using var context = CreateContext();
        var command = new AddDbInstanceCommand(context);
        var model = new AddDbInstanceModelStub
        {
            Name = "Test Instance",
            DatabaseTemplate = "Minimal"
        };

        var result = command.Execute(model);

        result.Id.ShouldBeGreaterThan(0);
        context.DbInstances.Any(d => d.Id == result.Id).ShouldBeTrue();
    }

    [Test]
    public void Execute_WithValidModel_SetsExpectedFieldValues()
    {
        using var context = CreateContext();
        var command = new AddDbInstanceCommand(context);
        var before = DateTime.UtcNow;
        var model = new AddDbInstanceModelStub
        {
            Name = "  Test Instance  ",
            DatabaseTemplate = " Minimal "
        };

        var result = command.Execute(model);

        result.Name.ShouldBe("Test Instance");
        result.DatabaseTemplate.ShouldBe("Minimal");
        result.Status.ShouldBe(DbInstanceStatus.Pending.ToString());
        result.OdsInstanceId.ShouldBeNull();
        result.OdsInstanceName.ShouldBeNull();
        result.DatabaseName.ShouldBeNull();
        result.LastRefreshed.ShouldBeGreaterThanOrEqualTo(before);
        result.LastModifiedDate.ShouldNotBeNull();
        result.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public void Execute_WithSampleTemplate_PersistsWithCorrectTemplate()
    {
        using var context = CreateContext();
        var command = new AddDbInstanceCommand(context);
        var model = new AddDbInstanceModelStub
        {
            Name = "Sample Instance",
            DatabaseTemplate = "Sample"
        };

        var result = command.Execute(model);

        result.DatabaseTemplate.ShouldBe("Sample");
        context.DbInstances.Any(d => d.Id == result.Id && d.DatabaseTemplate == "Sample").ShouldBeTrue();
    }

    private sealed class AddDbInstanceModelStub : IAddDbInstanceModel
    {
        public string? Name { get; set; }
        public string? DatabaseTemplate { get; set; }
    }
}

#nullable restore
