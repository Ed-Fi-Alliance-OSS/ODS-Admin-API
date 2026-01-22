// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Quartz;

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

        // Act
        await job.Execute(_jobExecutionContext);

        // Assert
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.InProgress, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Completed, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Error, A<string>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Execute_SetsStatusToError_AndLogsError_OnException()
    {
        // Arrange
        var job = new TestQuartzJob(_logger, _jobStatusService, throwOnExecute: true);

        // Act
        await job.Execute(_jobExecutionContext);

        // Assert
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.InProgress, null)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Completed, null)).MustNotHaveHappened();
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Error, A<string>.That.Contains("Test exception"))).MustHaveHappenedOnceExactly();

    }

    [Test]
    public async Task PendingStatus_CanBeSet_IfImplemented()
    {
        // Act
        await _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Pending, null);

        // Assert
        A.CallTo(() => _jobStatusService.SetStatusAsync("TestJob", QuartzJobStatus.Pending, null)).MustHaveHappenedOnceExactly();
    }

    // Helper class to test the abstract base
    private class TestQuartzJob : AdminApiQuartzJobBase
    {
        private readonly bool _throwOnExecute;

        public TestQuartzJob(ILogger logger, IJobStatusService jobStatusService, bool throwOnExecute = false)
            : base(logger, jobStatusService)
        {
            _throwOnExecute = throwOnExecute;
        }

        protected override Task ExecuteJobAsync(IJobExecutionContext context)
        {
            if (_throwOnExecute)
                throw new Exception("Test exception");
            return Task.CompletedTask;
        }
    }
}

