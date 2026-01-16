// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.Extensions.Logging;
using Quartz;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Jobs
{
    public abstract class AdminApiQuartzJobBase(ILogger logger, IJobStatusService jobStatusService) : IJob
    {
        private readonly ILogger _logger = logger;
        private readonly IJobStatusService _jobStatusService = jobStatusService;

        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.Key.Name;
            try
            {
                await _jobStatusService.SetStatusAsync(jobId, QuartzJobStatus.InProgress);
                await ExecuteJobAsync(context);
                await _jobStatusService.SetStatusAsync(jobId, QuartzJobStatus.Completed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId} failed.", jobId);
                await _jobStatusService.SetStatusAsync(jobId, QuartzJobStatus.Error, ex.Message);
            }
        }

        protected abstract Task ExecuteJobAsync(IJobExecutionContext context);
    }

    public enum QuartzJobStatus
    {
        Pending,
        InProgress,
        Completed,
        Error
    }
}
