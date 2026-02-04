// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Quartz;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Jobs;

[TestFixture]
public class AdminApiQuartzJobBaseTests
{
    private ILogger _logger;
    private IJobStatusService _jobStatusService;
    private IJobExecutionContext _jobExecutionContext;

    [SetUp]
    public void SetUp()
    {
        _logger = A.Fake<ILogger>();
        _jobStatusService = A.Fake<IJobStatusService>();
        _jobExecutionContext = A.Fake<IJobExecutionContext>();
        var jobKey = new JobKey("TestJob");
        var jobDetail = A.Fake<IJobDetail>();
        A.CallTo(() => jobDetail.Key).Returns(jobKey);
        A.CallTo(() => _jobExecutionContext.JobDetail).Returns(jobDetail);
    }

    [Test]
    public async Task Execute_SetsStatusToInProgressAndCompleted_OnSuccess()
    {
        // Arrange
        var job = new TestQuartzJob(_logger, _jobStatusService);
        string? capturedId = null;
        A.CallTo(
                () =>
                    _jobStatusService.SetStatusAsync(
                        A<string>.That.Matches(id => id.StartsWith("TestJob")),
                        QuartzJobStatus.InProgress,
                        "",
                        null
                    )
            )
            .Invokes(
                (string id, QuartzJobStatus status, string? tenantName, string? errorMessage) =>
                    capturedId = id
            );

        // Act
        await job.Execute(_jobExecutionContext);

        // Assert
        // Ensure the id was captured and matches the expected format
        capturedId.ShouldNotBeNull();
        capturedId!.ShouldStartWith("TestJob");
        // Ensure InProgress was set with the captured id
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.InProgress, "", null))
            .MustHaveHappenedOnceExactly();
        // Ensure Completed was set with the same id
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.Completed, "", null))
            .MustHaveHappenedOnceExactly();
        // Ensure Error was never set with the same id
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.Error, "", A<string>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task Execute_SetsStatusToError_AndLogsError_OnException()
    {
        // Arrange
        var job = new TestQuartzJob(_logger, _jobStatusService, throwOnExecute: true);
        string? capturedId = null;
        A.CallTo(
                () =>
                    _jobStatusService.SetStatusAsync(
                        A<string>.That.Matches(id => id.StartsWith("TestJob")),
                        QuartzJobStatus.InProgress,
                        "",
                        null
                    )
            )
            .Invokes(
                (string id, QuartzJobStatus status, string? tenantName, string? errorMessage) =>
                    capturedId = id
            );

        // Act
        await job.Execute(_jobExecutionContext);

        // Assert
        capturedId.ShouldNotBeNull();
        capturedId!.ShouldStartWith("TestJob");
        // Ensure InProgress was set with the captured id
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.InProgress, "", null))
            .MustHaveHappenedOnceExactly();
        // Ensure Completed was never set with the same id
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.Completed, "", null))
            .MustNotHaveHappened();
        // Ensure Error was set with the same id and correct message
        A.CallTo(
                () =>
                    _jobStatusService.SetStatusAsync(
                        capturedId,
                        QuartzJobStatus.Error,
                        "",
                        A<string>.That.Contains("Test exception")
                    )
            )
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task PendingStatus_CanBeSet_IfImplemented()
    {
        // Act
        await _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Pending, null, null);

        // Assert
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Pending, null, null))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_SetsStatus_WithTenantName()
    {
        // Arrange
        var job = new TestQuartzJob(_logger, _jobStatusService);

        // Simulate a tenant name in the job data map
        var jobDataMap = new JobDataMap { { "TenantName", "tenant1" } };
        A.CallTo(() => _jobExecutionContext.MergedJobDataMap).Returns(jobDataMap);

        string? capturedId = null;
        string? capturedTenant = null;
        A.CallTo(
                () =>
                    _jobStatusService.SetStatusAsync(
                        A<string>.That.Matches(id => id.StartsWith("TestJob")),
                        QuartzJobStatus.InProgress,
                        "tenant1",
                        null
                    )
            )
            .Invokes(
                (string id, QuartzJobStatus status, string? tenantName, string? errorMessage) =>
                {
                    capturedId = id;
                    capturedTenant = tenantName;
                }
            );

        // Act
        await job.Execute(_jobExecutionContext);

        // Assert
        capturedId.ShouldNotBeNull();
        capturedTenant.ShouldBe("tenant1");
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.InProgress, "tenant1", null))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.Completed, "tenant1", null))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _jobStatusService.SetStatusAsync(capturedId, QuartzJobStatus.Error, "tenant1", A<string>._))
            .MustNotHaveHappened();
    }


    // Helper class to test the abstract base
    private class TestQuartzJob(ILogger logger, IJobStatusService jobStatusService, bool throwOnExecute = false) : AdminApiQuartzJobBase(logger, jobStatusService)
    {
        private readonly bool _throwOnExecute = throwOnExecute;

        protected override Task ExecuteJobAsync(IJobExecutionContext context)
        {
            if (_throwOnExecute)
                throw new Exception("Test exception");
            return Task.CompletedTask;
        }
    }
}
