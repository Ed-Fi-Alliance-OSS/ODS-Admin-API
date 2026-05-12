// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DbInstances;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DbInstances;

[TestFixture]
public class DeleteDbInstanceTests
{
    private IGetDbInstanceByIdQuery _getDbInstanceByIdQuery = null!;
    private IDeleteDbInstanceCommand _deleteDbInstanceCommand = null!;
    private ISchedulerFactory _schedulerFactory = null!;
    private IContextProvider<TenantConfiguration> _tenantConfigurationProvider = null!;
    private IOptions<AppSettings> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _getDbInstanceByIdQuery = A.Fake<IGetDbInstanceByIdQuery>();
        _deleteDbInstanceCommand = A.Fake<IDeleteDbInstanceCommand>();

        var scheduler = A.Fake<IScheduler>();
        A.CallTo(() => scheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        _schedulerFactory = A.Fake<ISchedulerFactory>();
        A.CallTo(() => _schedulerFactory.GetScheduler(A<CancellationToken>._))
            .Returns(Task.FromResult(scheduler));

        _tenantConfigurationProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        A.CallTo(() => _tenantConfigurationProvider.Get()).Returns(null);

        _options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
    }

    private static HttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 7214);
        return httpContext;
    }

    private Task<IResult> Handle(int id)
        => DeleteDbInstance.Handle(
            _getDbInstanceByIdQuery,
            _deleteDbInstanceCommand,
            _schedulerFactory,
            _tenantConfigurationProvider,
            _options,
            id,
            CreateHttpContext());

    [Test]
    public async Task Handle_WhenDbInstanceNotFound_ThrowsNotFoundException()
    {
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(99)).Returns(null);

        await Should.ThrowAsync<NotFoundException<int>>(() => Handle(99));
    }

    [Test]
    public async Task Handle_WhenStatusIsCreated_ExecutesCommandAndReturnsAccepted()
    {
        var dbInstance = new DbInstance
        {
            Id = 1,
            Name = "Test",
            Status = DbInstanceStatus.Created.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(1)).Returns(dbInstance);

        var result = await Handle(1);

        result.ShouldBeOfType<Accepted>();
        A.CallTo(() => _deleteDbInstanceCommand.Execute(1)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenStatusIsPendingCreate_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 2,
            Name = "Test",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(2)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(2));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("provisioned"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCreateInProgress_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 3,
            Name = "Test",
            Status = DbInstanceStatus.CreateInProgress.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(3)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(3));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("provisioned"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCreateFailed_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 4,
            Name = "Test",
            Status = DbInstanceStatus.CreateFailed.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(4)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(4));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("creation failed"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCreateError_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 5,
            Name = "Test",
            Status = DbInstanceStatus.CreateError.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(5)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(5));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("creation failed permanently"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsPendingDelete_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 6,
            Name = "Test",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(6)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(6));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("queued for deletion"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteInProgress_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 7,
            Name = "Test",
            Status = DbInstanceStatus.DeleteInProgress.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(7)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(7));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("currently being deleted"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteFailed_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 8,
            Name = "Test",
            Status = DbInstanceStatus.DeleteFailed.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(8)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(8));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("retried automatically"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteError_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 9,
            Name = "Test",
            Status = DbInstanceStatus.DeleteError.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(9)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(9));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("deletion failed permanently"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleted_ThrowsNotFoundException()
    {
        var dbInstance = new DbInstance
        {
            Id = 10,
            Name = "Test",
            Status = DbInstanceStatus.Deleted.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(10)).Returns(dbInstance);

        await Should.ThrowAsync<NotFoundException<int>>(() => Handle(10));

        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }
}

#nullable restore

