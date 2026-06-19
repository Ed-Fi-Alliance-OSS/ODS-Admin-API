// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Jobs;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Jobs;

[TestFixture]
public class GetJobStatusTests
{
    private IJobStatusService _jobStatusService = null!;
    private IContextProvider<TenantConfiguration> _tenantConfigurationProvider = null!;
    private IOptions<AppSettings> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _jobStatusService = A.Fake<IJobStatusService>();
        _tenantConfigurationProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        _options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => _options.Value).Returns(new AppSettings { MultiTenancy = false });
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
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options);

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
    public async Task Handle_Returns404NotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = "nonexistent-job-id";
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns((JobStatus?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException<string>>(
            () => GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options));
    }

    [Test]
    public async Task Handle_ThrowsException_WhenServiceThrowsException()
    {
        // Arrange
        var jobId = "job-causing-error";
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Throws(new InvalidOperationException("Unexpected database error"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options));
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
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult!.Value!.FinishedAt.ShouldBe(finishedAt);
        okResult!.Value!.ErrorMessage.ShouldBeNull();
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
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult!.Value!.Status.ShouldBe("Error");
        okResult!.Value!.ErrorMessage.ShouldBe(errorMsg);
        okResult!.Value!.FinishedAt.ShouldBe(finishedAt);
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
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult!.Value!.Status.ShouldBe("Pending");
        okResult!.Value!.FinishedAt.ShouldBeNull();
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
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        okResult!.Value!.Status.ShouldBe("InProgress");
        okResult!.Value!.FinishedAt.ShouldBeNull();
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
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId, A<string?>.Ignored))
            .Returns(jobStatus);

        // Act
        var result = await GetJobStatus.Handle(jobId, _jobStatusService, _tenantConfigurationProvider, _options);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<GetJobStatus.Response>;
        okResult.ShouldNotBeNull();
        var response = okResult!.Value!;
        response.JobId.ShouldNotBeNullOrEmpty();
        response.Status.ShouldNotBeNullOrEmpty();
        response.CreatedAt.ShouldNotBe(default);
        response.FinishedAt.ShouldNotBeNull();
    }
}
