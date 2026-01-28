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
            var runId = $"{jobId}_{context.FireInstanceId}";
            var tenantName = context.MergedJobDataMap.ContainsKey(JobConstants.TenantNameKey) ? context.MergedJobDataMap.GetString(JobConstants.TenantNameKey) : string.Empty;
            try
            {
                await _jobStatusService.SetStatusAsync(runId, QuartzJobStatus.InProgress, tenantName);
                await ExecuteJobAsync(context);
                await _jobStatusService.SetStatusAsync(runId, QuartzJobStatus.Completed, tenantName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId} with Run {RunId} failed.", jobId, runId);
                await _jobStatusService.SetStatusAsync(runId, QuartzJobStatus.Error, tenantName, ex.Message);
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
