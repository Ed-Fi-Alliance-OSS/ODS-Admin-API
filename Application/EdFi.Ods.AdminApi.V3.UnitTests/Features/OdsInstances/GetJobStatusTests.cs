// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.V3.Features.OdsInstances;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.OdsInstances;

[TestFixture]
public class GetJobStatusTests
{
    private IJobStatusService _jobStatusService = null!;

    [SetUp]
    public void SetUp()
    {
        _jobStatusService = A.Fake<IJobStatusService>();
    }

    [Test]
    public async Task Handle_ReturnsOkWithJobDetails_WhenJobExists()
    {
        // Arrange
        var jobId = "RefreshEducationOrganizationsJob-tenant-123_fireinstance-456";
        var createdAt = new DateTime(2026, 5, 15, 22, 53, 47, 78);
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Pending",
            CreatedAt = createdAt,
            FinishedAt = null,
            ErrorMessage = null
        };
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.JobId.ShouldBe(jobId);
        okResult.Value.Status.ShouldBe("Pending");
        okResult.Value.CreatedAt.ShouldBe(createdAt);
        okResult.Value.FinishedAt.ShouldBeNull();
        okResult.Value.ErrorMessage.ShouldBeNull();
    }

    [Test]
    public async Task Handle_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = "nonexistent-job-id";
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns((JobStatus?)null);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        result.ShouldNotBeNull();
        // Verify it's a NotFound result (not Ok)
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldBeNull("Expected NotFound result, not Ok");
    }

    [Test]
    public async Task Handle_IncludesFinishedAt_WhenJobIsCompleted()
    {
        // Arrange
        var jobId = "job-completed-123";
        var createdAt = new DateTime(2026, 5, 15, 22, 53, 47);
        var finishedAt = new DateTime(2026, 5, 15, 23, 00, 00);
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Completed",
            CreatedAt = createdAt,
            FinishedAt = finishedAt,
            ErrorMessage = null
        };
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.FinishedAt.ShouldBe(finishedAt);
        okResult.Value.ErrorMessage.ShouldBeNull();
    }

    [Test]
    public async Task Handle_IncludesErrorMessage_WhenJobHasError()
    {
        // Arrange
        var jobId = "job-error-123";
        var errorMsg = "Database connection failed";
        var createdAt = new DateTime(2026, 5, 15, 22, 53, 47);
        var finishedAt = new DateTime(2026, 5, 15, 23, 00, 00);
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Error",
            CreatedAt = createdAt,
            FinishedAt = finishedAt,
            ErrorMessage = errorMsg
        };
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.Status.ShouldBe("Error");
        okResult.Value.ErrorMessage.ShouldBe(errorMsg);
        okResult.Value.FinishedAt.ShouldBe(finishedAt);
    }

    [Test]
    public async Task Handle_HasNullFinishedAt_WhenJobIsPending()
    {
        // Arrange
        var jobId = "job-pending-123";
        var createdAt = new DateTime(2026, 5, 15, 22, 53, 47);
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Pending",
            CreatedAt = createdAt,
            FinishedAt = null,
            ErrorMessage = null
        };
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.Status.ShouldBe("Pending");
        okResult.Value.FinishedAt.ShouldBeNull();
    }

    [Test]
    public async Task Handle_HasNullFinishedAt_WhenJobIsInProgress()
    {
        // Arrange
        var jobId = "job-inprogress-123";
        var createdAt = new DateTime(2026, 5, 15, 22, 53, 47);
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "InProgress",
            CreatedAt = createdAt,
            FinishedAt = null,
            ErrorMessage = null
        };
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult.Value.Status.ShouldBe("InProgress");
        okResult.Value.FinishedAt.ShouldBeNull();
    }

    [Test]
    public async Task Handle_ReturnsAllResponseFields()
    {
        // Arrange
        var jobId = "job-123";
        var createdAt = new DateTime(2026, 5, 15, 22, 53, 47);
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Completed",
            CreatedAt = createdAt,
            FinishedAt = new DateTime(2026, 5, 15, 23, 00, 00),
            ErrorMessage = null
        };
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        var response = okResult.Value;
        response.JobId.ShouldNotBeNullOrEmpty();
        response.Status.ShouldNotBeNullOrEmpty();
        response.CreatedAt.ShouldNotBe(default);
        response.FinishedAt.ShouldNotBeNull();
    }
}
