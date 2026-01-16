// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Quartz;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;

public static class QuartzJobScheduler
{
    public static async Task ScheduleJob<TJob>(
        IScheduler scheduler,
        string jobId,
        IDictionary<string, object> jobData,
        bool startImmediately = true,
        TimeSpan? interval = null)
        where TJob : IJob
    {
        var job = JobBuilder.Create<TJob>()
            .WithIdentity(jobId)
            .UsingJobData([.. jobData])
            .Build();

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
    }
}
