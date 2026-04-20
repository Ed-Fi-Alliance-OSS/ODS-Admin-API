// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.DbInstances;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.Features.DbInstances;

[TestFixture]
public class AddDbInstanceTests
{
    private static EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"AddDbInstance_{Guid.NewGuid()}")
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = "SqlServer"
            })
            .Build();
        return new EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext(options, configuration);
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsAccepted()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        var result = await AddDbInstance.Handle(validator, command, request);

        result.ShouldBeOfType<Accepted>();
    }

    [Test]
    public async Task Handle_WithValidRequest_PersistsDbInstance()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Sample"
        };

        await AddDbInstance.Handle(validator, command, request);

        context.DbInstances.Any(d => d.Name == "My DB Instance").ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = string.Empty,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request));
    }

    [Test]
    public async Task Handle_WithNullName_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = null,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request));
    }

    [Test]
    public async Task Handle_WithNameExceedingMaxLength_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = new string('a', 101),
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request));
    }

    [Test]
    public async Task Handle_WithEmptyDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = string.Empty
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request));
    }

    [Test]
    public async Task Handle_WithNullDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = null
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request));
    }

    [Test]
    public async Task Handle_WithInvalidDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "InvalidTemplate"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request));
    }
}

#nullable restore
