// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Services.Jobs;

[TestFixture]
public class DeletePendingDataStoreManagesDispatcherJobTests
{
    private sealed class NonDisposingAdminApiDbContext(
        DbContextOptions<AdminApiDbContext> options,
        IConfiguration configuration)
        : AdminApiDbContext(options, configuration)
    {
        public override void Dispose() { }

        public override ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private static AdminApiDbContext CreateAdminApiContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AppSettings:DatabaseEngine"] = "SqlServer"
            })
            .Build();

        return new NonDisposingAdminApiDbContext(options, configuration);
    }

    private static IOptions<AppSettings> CreateOptions(bool multiTenancy = false, int maxRetryAttempts = 3)
        => Options.Create(new AppSettings
        {
            DatabaseEngine = "SqlServer",
            MultiTenancy = multiTenancy,
            DeleteOdsInstanceManagesMaxRetryAttempts = maxRetryAttempts
        });

    private static IScheduler CreateScheduler(out List<IJobDetail> scheduledJobs)
    {
        var capturedScheduledJobs = new List<IJobDetail>();
        scheduledJobs = capturedScheduledJobs;
        var scheduler = A.Fake<IScheduler>();

        A.CallTo(() => scheduler.GetJobDetail(A<JobKey>._, A<CancellationToken>._))
            .Returns(Task.FromResult((IJobDetail)null));
        A.CallTo(() => scheduler.ScheduleJob(A<IJobDetail>._, A<ITrigger>._, A<CancellationToken>._))
            .Invokes((IJobDetail job, ITrigger _, CancellationToken _) => capturedScheduledJobs.Add(job))
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));

        return scheduler;
    }

    private static IJobExecutionContext CreateJobExecutionContext(IScheduler scheduler, string tenantName = null)
    {
        var jobExecutionContext = A.Fake<IJobExecutionContext>();
        var jobDetail = A.Fake<IJobDetail>();
        var jobDataMap = new JobDataMap();

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobDataMap.Put(JobConstants.TenantNameKey, tenantName);
        }

        A.CallTo(() => jobDetail.Key).Returns(new JobKey(JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName));
        A.CallTo(() => jobExecutionContext.JobDetail).Returns(jobDetail);
        A.CallTo(() => jobExecutionContext.FireInstanceId).Returns(Guid.NewGuid().ToString());
        A.CallTo(() => jobExecutionContext.MergedJobDataMap).Returns(jobDataMap);
        A.CallTo(() => jobExecutionContext.Scheduler).Returns(scheduler);

        return jobExecutionContext;
    }

    [Test]
    public async Task Execute_SchedulesPendingDeleteOdsInstanceManage()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var jobStatusService = A.Fake<IJobStatusService>();
        var scheduler = CreateScheduler(out var scheduledJobs);

        adminApiContext.OdsInstanceManages.Add(new Common.Infrastructure.Models.OdsInstanceManage
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = OdsInstanceManageStatus.PendingDelete.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        });
        adminApiContext.SaveChanges();

        var job = new DeletePendingDataStoreManagesDispatcherJob(
            A.Fake<ILogger<DeletePendingDataStoreManagesDispatcherJob>>(),
            jobStatusService,
            adminApiContext,
            tenantSpecificDbContextProvider,
            CreateOptions());

        await job.Execute(CreateJobExecutionContext(scheduler));

        scheduledJobs.Count.ShouldBe(1);
        scheduledJobs[0].Key.Name.ShouldBe($"{JobConstants.DeleteInstanceJobName}-{adminApiContext.OdsInstanceManages.Single().Id}");
    }

    [Test]
    public async Task Execute_RequeuesRetryableDeleteFailedOdsInstanceManage()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var jobStatusService = A.Fake<IJobStatusService>();
        var scheduler = CreateScheduler(out var scheduledJobs);

        var odsInstanceManage = new Common.Infrastructure.Models.OdsInstanceManage
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = OdsInstanceManageStatus.DeleteFailed.ToString(),
            DatabaseName = "EdFi_Ods_Sandbox_Minimal",
            LastRefreshed = DateTime.UtcNow.AddMinutes(-10),
            LastModifiedDate = DateTime.UtcNow.AddMinutes(-10)
        };

        adminApiContext.OdsInstanceManages.Add(odsInstanceManage);
        adminApiContext.SaveChanges();
        adminApiContext.JobStatuses.Add(new JobStatus
        {
            JobId = $"{DeleteInstanceJob.BuildJobIdentity(odsInstanceManage.Id, null)}_run-1",
            Status = QuartzJobStatus.Error.ToString()
        });
        adminApiContext.SaveChanges();

        var job = new DeletePendingDataStoreManagesDispatcherJob(
            A.Fake<ILogger<DeletePendingDataStoreManagesDispatcherJob>>(),
            jobStatusService,
            adminApiContext,
            tenantSpecificDbContextProvider,
            CreateOptions(maxRetryAttempts: 3));

        await job.Execute(CreateJobExecutionContext(scheduler));

        adminApiContext.OdsInstanceManages.Single().Status.ShouldBe(OdsInstanceManageStatus.PendingDelete.ToString());
        scheduledJobs.Count.ShouldBe(1);
    }

    [Test]
    public async Task Execute_SetsDeleteError_WhenRetryLimitIsReached()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var jobStatusService = A.Fake<IJobStatusService>();
        var scheduler = CreateScheduler(out var scheduledJobs);

        var odsInstanceManage = new Common.Infrastructure.Models.OdsInstanceManage
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = OdsInstanceManageStatus.DeleteFailed.ToString(),
            LastRefreshed = DateTime.UtcNow.AddMinutes(-10),
            LastModifiedDate = DateTime.UtcNow.AddMinutes(-10)
        };

        adminApiContext.OdsInstanceManages.Add(odsInstanceManage);
        adminApiContext.SaveChanges();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            adminApiContext.JobStatuses.Add(new JobStatus
            {
                JobId = $"{DeleteInstanceJob.BuildJobIdentity(odsInstanceManage.Id, null)}_run-{attempt}",
                Status = QuartzJobStatus.Error.ToString()
            });
        }

        adminApiContext.SaveChanges();

        var job = new DeletePendingDataStoreManagesDispatcherJob(
            A.Fake<ILogger<DeletePendingDataStoreManagesDispatcherJob>>(),
            jobStatusService,
            adminApiContext,
            tenantSpecificDbContextProvider,
            CreateOptions(maxRetryAttempts: 3));

        await job.Execute(CreateJobExecutionContext(scheduler));

        adminApiContext.OdsInstanceManages.Single().Status.ShouldBe(OdsInstanceManageStatus.DeleteError.ToString());
        scheduledJobs.ShouldBeEmpty();
    }

    [Test]
    public async Task Execute_UsesTenantSpecificContext_WhenMultiTenancyIsEnabled()
    {
        using var defaultAdminApiContext = CreateAdminApiContext($"Admin_Default_{Guid.NewGuid()}");
        using var tenantAdminApiContext = CreateAdminApiContext($"Admin_Tenant_{Guid.NewGuid()}");
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var jobStatusService = A.Fake<IJobStatusService>();
        var scheduler = CreateScheduler(out var scheduledJobs);

        A.CallTo(() => tenantSpecificDbContextProvider.GetAdminApiDbContext("tenant1"))
            .Returns(tenantAdminApiContext);

        tenantAdminApiContext.OdsInstanceManages.Add(new Common.Infrastructure.Models.OdsInstanceManage
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = OdsInstanceManageStatus.PendingDelete.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        });
        tenantAdminApiContext.SaveChanges();

        var job = new DeletePendingDataStoreManagesDispatcherJob(
            A.Fake<ILogger<DeletePendingDataStoreManagesDispatcherJob>>(),
            jobStatusService,
            defaultAdminApiContext,
            tenantSpecificDbContextProvider,
            CreateOptions(multiTenancy: true));

        await job.Execute(CreateJobExecutionContext(scheduler, "tenant1"));

        scheduledJobs.Count.ShouldBe(1);
        scheduledJobs[0].Key.Name.ShouldBe($"{JobConstants.DeleteInstanceJobName}-tenant1-{tenantAdminApiContext.OdsInstanceManages.Single().Id}");
        scheduledJobs[0].JobDataMap.GetString(JobConstants.TenantNameKey).ShouldBe("tenant1");
    }

    [Test]
    public async Task Execute_Throws_WhenTenantNameMissingAndMultiTenancyEnabled()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var jobStatusService = A.Fake<IJobStatusService>();
        var scheduler = CreateScheduler(out _);

        var job = new DeletePendingDataStoreManagesDispatcherJob(
            A.Fake<ILogger<DeletePendingDataStoreManagesDispatcherJob>>(),
            jobStatusService,
            adminApiContext,
            tenantSpecificDbContextProvider,
            CreateOptions(multiTenancy: true));

        // Context has no TenantNameKey — base class swallows the exception and records Error status
        await job.Execute(CreateJobExecutionContext(scheduler, tenantName: null));

        A.CallTo(() => jobStatusService.SetStatusAsync(
                A<string>._,
                QuartzJobStatus.Error,
                A<string>._,
                A<string>._))
            .MustHaveHappenedOnceExactly();
    }
}
