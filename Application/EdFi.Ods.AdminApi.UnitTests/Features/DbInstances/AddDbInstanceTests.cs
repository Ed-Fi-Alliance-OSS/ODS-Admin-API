// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.DbInstances;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.Features.DbInstances;

[TestFixture]
public class AddDbInstanceTests
{
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

    private static IOptions<AppSettings> CreateOptions(bool multiTenancy = false)
        => Options.Create(new AppSettings { MultiTenancy = multiTenancy });

    private static IContextProvider<TenantConfiguration> CreateTenantConfigurationProvider(string? tenantIdentifier = null)
    {
        var provider = A.Fake<IContextProvider<TenantConfiguration>>();
        A.CallTo(() => provider.Get()).Returns(
            tenantIdentifier is null
                ? null
                : new TenantConfiguration { TenantIdentifier = tenantIdentifier });

        return provider;
    }

    private static ISchedulerFactory CreateSchedulerFactory(out IScheduler scheduler)
    {
        var createdScheduler = A.Fake<IScheduler>();

        var schedulerFactory = A.Fake<ISchedulerFactory>();
        A.CallTo(() => schedulerFactory.GetScheduler(A<CancellationToken>._))
            .Returns(Task.FromResult(createdScheduler));
        A.CallTo(() => createdScheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        scheduler = createdScheduler;

        return schedulerFactory;
    }

    [Test]
    public async Task Handle_WithValidRequest_ReturnsAccepted()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        var result = await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request);

        result.ShouldBeOfType<Accepted>();
    }

    [Test]
    public async Task Handle_WithValidRequest_PersistsDbInstance()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Sample"
        };

        await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request);

        context.DbInstances.Any(d => d.Name == "My DB Instance").ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithValidRequest_SchedulesCreateInstanceJob()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out var scheduler);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        IJobDetail? scheduledJob = null;

        A.CallTo(() => scheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Invokes((IJobDetail job, ITrigger _, CancellationToken _) => scheduledJob = job)
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request);

        var dbInstance = context.DbInstances.Single();

        scheduledJob.ShouldNotBeNull();
        scheduledJob!.Key.Name.ShouldBe($"{JobConstants.CreateInstanceJobName}-{dbInstance.Id}");
        scheduledJob.JobDataMap.GetInt(JobConstants.DbInstanceIdKey).ShouldBe(dbInstance.Id);
    }

    [Test]
    public async Task Handle_WithMultiTenancyEnabled_SchedulesTenantAwareCreateInstanceJob()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out var scheduler);
        var tenantProvider = CreateTenantConfigurationProvider("tenant1");
        var options = CreateOptions(multiTenancy: true);
        IJobDetail? scheduledJob = null;

        A.CallTo(() => scheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Invokes((IJobDetail job, ITrigger _, CancellationToken _) => scheduledJob = job)
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request);

        var dbInstance = context.DbInstances.Single();

        scheduledJob.ShouldNotBeNull();
        scheduledJob!.Key.Name.ShouldBe($"{JobConstants.CreateInstanceJobName}-tenant1-{dbInstance.Id}");
        scheduledJob.JobDataMap.GetString(JobConstants.TenantNameKey).ShouldBe("tenant1");
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = string.Empty,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request));
    }

    [Test]
    public async Task Handle_WithNullName_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = null,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request));
    }

    [Test]
    public async Task Handle_WithNameExceedingMaxLength_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = new string('a', 101),
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request));
    }

    [Test]
    public async Task Handle_WithEmptyDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = string.Empty
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request));
    }

    [Test]
    public async Task Handle_WithNullDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = null
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request));
    }

    [Test]
    public async Task Handle_WithInvalidDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        var validator = new AddDbInstance.Validator();
        var command = new AddDbInstanceCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var request = new AddDbInstance.AddDbInstanceRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "InvalidTemplate"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDbInstance.Handle(validator, command, schedulerFactory, tenantProvider, options, request));
    }
}

#nullable restore
