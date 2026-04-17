// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.V2.Features.DbInstances;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.V2.Features.DbInstances;

[TestFixture]
public class DeleteDbInstanceTests
{
    private IGetDbInstanceByIdQuery _getDbInstanceByIdQuery = null!;
    private IDeleteDbInstanceCommand _deleteDbInstanceCommand = null!;

    [SetUp]
    public void SetUp()
    {
        _getDbInstanceByIdQuery = A.Fake<IGetDbInstanceByIdQuery>();
        _deleteDbInstanceCommand = A.Fake<IDeleteDbInstanceCommand>();
    }

    [Test]
    public async Task Handle_WhenDbInstanceNotFound_ThrowsNotFoundException()
    {
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(99)).Returns(null);

        await Should.ThrowAsync<NotFoundException<int>>(() =>
            DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 99)
        );
    }

    [Test]
    public async Task Handle_WhenStatusIsPending_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 1,
            Name = "Test",
            Status = DbInstanceStatus.Pending.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(1)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() =>
            DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 1)
        );

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("provisioned"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsInProgress_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 2,
            Name = "Test",
            Status = DbInstanceStatus.InProgress.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(2)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(
            () => DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 2)
        );

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("provisioned"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsCompleted_ExecutesCommandAndReturnsNoContent()
    {
        var dbInstance = new DbInstance
        {
            Id = 3,
            Name = "Test",
            Status = DbInstanceStatus.Completed.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(3)).Returns(dbInstance);

        var result = await DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 3);

        result.ShouldBeOfType<NoContent>();
        A.CallTo(() => _deleteDbInstanceCommand.Execute(3)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleteFailed_ExecutesCommandAndReturnsNoContent()
    {
        var dbInstance = new DbInstance
        {
            Id = 4,
            Name = "Test",
            Status = DbInstanceStatus.DeleteFailed.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(4)).Returns(dbInstance);

        var result = await DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 4);

        result.ShouldBeOfType<NoContent>();
        A.CallTo(() => _deleteDbInstanceCommand.Execute(4)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_WhenStatusIsPendingDelete_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 5,
            Name = "Test",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(5)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() =>
            DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 5)
        );

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("queued for deletion"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsError_ThrowsValidationException()
    {
        var dbInstance = new DbInstance
        {
            Id = 6,
            Name = "Test",
            Status = DbInstanceStatus.Error.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(6)).Returns(dbInstance);

        var ex = await Should.ThrowAsync<ValidationException>(() =>
            DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 6)
        );

        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("error during provisioning"));
        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_WhenStatusIsDeleted_ThrowsNotFoundException()
    {
        var dbInstance = new DbInstance
        {
            Id = 7,
            Name = "Test",
            Status = DbInstanceStatus.Deleted.ToString(),
            DatabaseTemplate = "Minimal",
        };
        A.CallTo(() => _getDbInstanceByIdQuery.Execute(7)).Returns(dbInstance);

        await Should.ThrowAsync<NotFoundException<int>>(() =>
            DeleteDbInstance.Handle(_getDbInstanceByIdQuery, _deleteDbInstanceCommand, 7)
        );

        A.CallTo(() => _deleteDbInstanceCommand.Execute(A<int>._)).MustNotHaveHappened();
    }
}

#nullable restore
