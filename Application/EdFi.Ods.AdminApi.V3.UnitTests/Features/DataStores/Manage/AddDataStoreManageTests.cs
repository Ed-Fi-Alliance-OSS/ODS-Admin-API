// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores.Manage;

[TestFixture]
public class AddDataStoreManageTests
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
            .UseInMemoryDatabase(databaseName: $"AddDataStoreManage_{Guid.NewGuid()}")
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

    private static SqlServerUsersContext CreateUsersContext()
    {
        var options = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddDataStoreManageUsers_{Guid.NewGuid()}")
            .Options;

        return new SqlServerUsersContext(options);
    }

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
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        var result = await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext);

        result.ShouldBeOfType<Accepted>();
    }

    [Test]
    public async Task Handle_WithValidRequest_PersistsOdsInstanceManage()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Sample"
        };

        await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext);

        context.OdsInstanceManages.Any(d => d.Name == "My DB Instance").ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithValidRequest_SchedulesCreateInstanceJob()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out var scheduler);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        IJobDetail? scheduledJob = null;

        A.CallTo(() => scheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Invokes((IJobDetail job, ITrigger _, CancellationToken _) => scheduledJob = job)
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext);

        var odsInstanceManage = context.OdsInstanceManages.Single();

        scheduledJob.ShouldNotBeNull();
        scheduledJob!.Key.Name.ShouldBe($"{JobConstants.CreateInstanceJobName}-{odsInstanceManage.Id}");
        scheduledJob.JobDataMap.GetInt(JobConstants.OdsInstanceManageIdKey).ShouldBe(odsInstanceManage.Id);
    }

    [Test]
    public async Task Handle_WithMultiTenancyEnabled_SchedulesTenantAwareCreateInstanceJob()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out var scheduler);
        var tenantProvider = CreateTenantConfigurationProvider("tenant1");
        var options = CreateOptions(multiTenancy: true);
        var httpContext = CreateHttpContext();
        IJobDetail? scheduledJob = null;

        A.CallTo(() => scheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Invokes((IJobDetail job, ITrigger _, CancellationToken _) => scheduledJob = job)
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "Minimal"
        };

        await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext);

        var odsInstanceManage = context.OdsInstanceManages.Single();

        scheduledJob.ShouldNotBeNull();
        scheduledJob!.Key.Name.ShouldBe($"{JobConstants.CreateInstanceJobName}-tenant1-{odsInstanceManage.Id}");
        scheduledJob.JobDataMap.GetString(JobConstants.TenantNameKey).ShouldBe("tenant1");
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = string.Empty,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithNullName_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = null,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithNameExceedingMaxLength_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = new string('a', 101),
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithNameAtPortableDatabaseNameLimit_ReturnsAccepted()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = new string('a', 46),
            DatabaseTemplate = "Minimal"
        };

        var result = await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext);

        result.ShouldBeOfType<Accepted>();
    }

    [Test]
    public async Task Handle_WithFormattedDatabaseNameExceedingPortableLimit_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = new string('a', 47),
            DatabaseTemplate = "Minimal"
        };

        var exception = await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));

        exception.Errors.ShouldContain(error =>
            error.PropertyName == nameof(AddDataStoreManage.AddDataStoreManageRequest.Name)
            && error.ErrorMessage.Contains("portable limit of 63 characters"));
    }

    [TestCase("My-DB-Instance")]
    [TestCase("My.DB.Instance")]
    [TestCase("My/DB/Instance")]
    public async Task Handle_WithInvalidNameCharacters_ThrowsValidationException(string name)
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = name,
            DatabaseTemplate = "Minimal"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithEmptyDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = string.Empty
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithNullDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = null
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithInvalidDatabaseTemplate_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();
        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "My DB Instance",
            DatabaseTemplate = "InvalidTemplate"
        };

        await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));
    }

    [Test]
    public async Task Handle_WithExistingOdsInstanceManageName_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();

        context.OdsInstanceManages.Add(new Common.Infrastructure.Models.OdsInstanceManage
        {
            Name = "Existing Instance",
            DatabaseTemplate = "Minimal",
            Status = "Pending",
            LastModifiedDate = DateTime.UtcNow,
            LastRefreshed = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "Existing Instance",
            DatabaseTemplate = "Minimal"
        };

        var exception = await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));

        exception.Errors.ShouldContain(error =>
            error.PropertyName == nameof(AddDataStoreManage.AddDataStoreManageRequest.Name)
            && error.ErrorMessage == "A DataStoreManage named 'Existing Instance' already exists.");
    }

    [Test]
    public async Task Handle_WithExistingOdsInstanceName_ThrowsValidationException()
    {
        using var context = CreateContext();
        using var usersContext = CreateUsersContext();
        var validator = new AddDataStoreManage.Validator(context, usersContext);
        var command = new AddDataStoreManageCommand(context);
        var schedulerFactory = CreateSchedulerFactory(out _);
        var tenantProvider = CreateTenantConfigurationProvider();
        var options = CreateOptions();
        var httpContext = CreateHttpContext();

        usersContext.OdsInstances.Add(new OdsInstance
        {
            Name = "Existing Instance",
            InstanceType = "Minimal",
            ConnectionString = "encrypted::existing"
        });
        await usersContext.SaveChangesAsync();

        var request = new AddDataStoreManage.AddDataStoreManageRequest
        {
            Name = "Existing Instance",
            DatabaseTemplate = "Minimal"
        };

        var exception = await Should.ThrowAsync<ValidationException>(async () => await AddDataStoreManage.Handle(validator, command, schedulerFactory, tenantProvider, options, request, httpContext));

        exception.Errors.ShouldContain(error =>
            error.PropertyName == nameof(AddDataStoreManage.AddDataStoreManageRequest.Name)
            && error.ErrorMessage == "A DataStore named 'Existing Instance' already exists.");
    }

    [Test]
    public async Task Handle_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await AddDataStoreManage.Handle(null!, null!, null!, null!, disabledOptions, null!, null!));

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
}

#nullable restore
