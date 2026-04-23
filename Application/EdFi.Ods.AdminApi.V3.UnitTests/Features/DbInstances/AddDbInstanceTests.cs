// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.DbInstances;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DbInstances;

[TestFixture]
public class AddDbInstanceTests
{
    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 7214);
        return httpContext;
    }

    private static AdminApiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"AddDbInstance_{Guid.NewGuid()}")
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
        var httpContext = CreateHttpContext();

        var result = await AddDbInstance.Handle(validator, command, request, httpContext);

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
        var httpContext = CreateHttpContext();

        await AddDbInstance.Handle(validator, command, request, httpContext);

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
        var httpContext = CreateHttpContext();

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request, httpContext));
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
        var httpContext = CreateHttpContext();

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request, httpContext));
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
        var httpContext = CreateHttpContext();

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request, httpContext));
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
        var httpContext = CreateHttpContext();

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request, httpContext));
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
        var httpContext = CreateHttpContext();

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request, httpContext));
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
        var httpContext = CreateHttpContext();

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, request, httpContext));
    }
}

#nullable restore


