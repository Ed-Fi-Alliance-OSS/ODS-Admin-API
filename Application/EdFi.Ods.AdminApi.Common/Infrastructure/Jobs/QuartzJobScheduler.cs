// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Quartz;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;

public static class QuartzJobScheduler
{
    /// <summary>
    /// Schedules the job, or returns the already-scheduled/running job of the same identity
    /// unchanged. Callers that embed a caller-generated id (e.g. RunIdKey) in <paramref name="jobData"/>
    /// must read it back from the returned <see cref="IJobDetail"/>'s JobDataMap rather than assuming
    /// their own value was used — it wasn't, when an existing job wins the race.
    /// </summary>
    public static async Task<IJobDetail> ScheduleJob<TJob>(
        IScheduler scheduler,
        JobKey jobKey,
        IDictionary<string, object> jobData,
        bool startImmediately = true,
        TimeSpan? interval = null)
        where TJob : IJob
    {
        var job = JobBuilder.Create<TJob>()
            .WithIdentity(jobKey)
            .UsingJobData([.. jobData])
            .Build();

        // Check if job already exists or is running
        var existingJob = await scheduler.GetJobDetail(jobKey);
        if (existingJob != null)
        {
            // Optionally, check if a trigger is currently running for this job
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            var states = await Task.WhenAll(triggers.Select(t => scheduler.GetTriggerState(t.Key)));
            if (states.Any(state => state == TriggerState.Normal || state == TriggerState.Blocked))
            {
                // Job is already scheduled or running - report the existing job, not the new one.
                return existingJob;
            }
        }

        ITrigger trigger;
        if (startImmediately)
        {
            trigger = TriggerBuilder.Create().StartNow().Build();
        }
        else if (interval.HasValue)
        {
            trigger = TriggerBuilder.Create()
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(interval.Value).RepeatForever())
                .Build();
        }
        else
        {
            throw new ArgumentException("Must specify startImmediately or interval.");
        }

        await scheduler.ScheduleJob(job, trigger);
        return job;
    }
}
