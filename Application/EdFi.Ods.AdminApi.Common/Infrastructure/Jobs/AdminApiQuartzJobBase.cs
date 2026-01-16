// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
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
                await _jobStatusService.SetStatusAsync(jobId, JobStatus.InProgress);
                await ExecuteJobAsync(context);
                await _jobStatusService.SetStatusAsync(jobId, JobStatus.Completed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId} failed.", jobId);
                await _jobStatusService.SetStatusAsync(jobId, JobStatus.Error, ex.Message);
            }
        }

        protected abstract Task ExecuteJobAsync(IJobExecutionContext context);
    }

    public enum JobStatus
    {
        Pending,
        InProgress,
        Completed,
        Error
    }
}
