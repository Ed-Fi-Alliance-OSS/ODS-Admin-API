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
using EdFi.Ods.AdminApi.Features.OdsInstances.Manage;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances.Manage;

[TestFixture]
public class DeleteOdsInstanceManageTests
{
    private IGetOdsInstanceManageByIdQuery _getOdsInstanceManageByIdQuery = null!;
    private IDeleteOdsInstanceManageCommand _deleteOdsInstanceManageCommand = null!;
    private ISchedulerFactory _schedulerFactory = null!;
    private IContextProvider<TenantConfiguration> _tenantConfigurationProvider = null!;
    private IOptions<AppSettings> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _getOdsInstanceManageByIdQuery = A.Fake<IGetOdsInstanceManageByIdQuery>();
        _deleteOdsInstanceManageCommand = A.Fake<IDeleteOdsInstanceManageCommand>();

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

    private Task<IResult> Handle(int id)
        => DeleteOdsInstanceManage.Handle(
            _getOdsInstanceManageByIdQuery,
            _deleteOdsInstanceManageCommand,
            _schedulerFactory,
            _tenantConfigurationProvider,
            _options,
            id);

    [Test]
    public async Task Handle_WhenOdsInstanceManageNotFound_ThrowsNotFoundException()
    {
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(99)).Returns(null);

        await Should.ThrowAsync<NotFoundException<int>>(() => Handle(99));
    }

    [Test]
    public async Task Handle_WhenStatusIsCreated_ExecutesCommandAndReturnsAccepted()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 1,
            Name = "Test",
            Status = OdsInstanceManageStatus.Created.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(1)).Returns(odsInstanceManage);

        var result = await Handle(1);

        result.ShouldBeOfType<NoContent>();
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(1)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenStatusIsPendingCreate_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 2,
            Name = "Test",
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(2)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(2));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("provisioned"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCreateInProgress_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 3,
            Name = "Test",
            Status = OdsInstanceManageStatus.CreateInProgress.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(3)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(3));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("provisioned"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCreateFailed_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 4,
            Name = "Test",
            Status = OdsInstanceManageStatus.CreateFailed.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(4)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(4));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("creation failed"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCreateError_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 5,
            Name = "Test",
            Status = OdsInstanceManageStatus.CreateError.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(5)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(5));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("creation failed permanently"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsPendingDelete_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 6,
            Name = "Test",
            Status = OdsInstanceManageStatus.PendingDelete.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(6)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(6));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("queued for deletion"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteInProgress_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 7,
            Name = "Test",
            Status = OdsInstanceManageStatus.DeleteInProgress.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(7)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(7));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("currently being deleted"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteFailed_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 8,
            Name = "Test",
            Status = OdsInstanceManageStatus.DeleteFailed.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(8)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(8));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("retried automatically"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteError_ThrowsValidationException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 9,
            Name = "Test",
            Status = OdsInstanceManageStatus.DeleteError.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(9)).Returns(odsInstanceManage);

        var ex = await Should.ThrowAsync<ValidationException>(() => Handle(9));

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("deletion failed permanently"));
        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleted_ThrowsNotFoundException()
    {
        var odsInstanceManage = new OdsInstanceManage
        {
            Id = 10,
            Name = "Test",
            Status = OdsInstanceManageStatus.Deleted.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getOdsInstanceManageByIdQuery.Execute(10)).Returns(odsInstanceManage);

        await Should.ThrowAsync<NotFoundException<int>>(() => Handle(10));

        A.CallTo(() => _deleteOdsInstanceManageCommand.Execute(A<int>._)).MustNotHaveHappened();
    }
}

#nullable restore

