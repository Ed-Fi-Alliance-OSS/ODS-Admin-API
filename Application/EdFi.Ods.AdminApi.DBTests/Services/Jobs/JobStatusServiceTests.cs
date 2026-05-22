// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using EdFi.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.DBTests.Services.Jobs;

[TestFixture]
public class JobStatusServiceTests : AdminApiDbContextTestBase
{
    private DbContextOptions<Infrastructure.AdminApiDbContext> _dbContextOptions = null!;

    [SetUp]
    public void SetUpService()
    {
        _dbContextOptions = GetAdminApiDbContextOptions(ConnectionString);
    }

    [Test]
    public async Task SetStatusAsync_CreatesNewStatus_WhenNotExists()
    {
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-1", QuartzJobStatus.InProgress, null, "No error");
        using var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await context.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-1");
        status.ShouldNotBeNull();
        status!.Status.ShouldBe(QuartzJobStatus.InProgress.ToString());
        status.ErrorMessage.ShouldBe("No error");
    }

    [Test]
    public async Task SetStatusAsync_UpdatesStatus_WhenExists()
    {
        using (var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()))
        {
            context.JobStatuses.Add(new JobStatus { JobId = "job-2", Status = "Pending", ErrorMessage = null });
            await context.SaveChangesAsync();
        }

        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-2", QuartzJobStatus.Completed, null, "Done");
        using var verifyContext = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await verifyContext.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-2");
        status.ShouldNotBeNull();
        status!.Status.ShouldBe(QuartzJobStatus.Completed.ToString());
        status.ErrorMessage.ShouldBe("Done");
    }

    [Test]
    public async Task SetStatusAsync_UsesTenantSpecificDbContext_WhenMultiTenancyEnabled()
    {
        var tenantName = "tenant1";
        var tenantDbContextOptions = GetAdminApiDbContextOptions(ConnectionString);
        var tenantDbContext = new Infrastructure.AdminApiDbContext(tenantDbContextOptions, Testing.Configuration());

        var provider = new DummyTenantSpecificDbContextProvider(_dbContextOptions, tenantName, tenantDbContext);

        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            provider,
            Options.Create(new AppSettings { MultiTenancy = true }));

        await service.SetStatusAsync("job-tenant1", QuartzJobStatus.InProgress, tenantName, "Tenant error");

        var status = await tenantDbContext.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-tenant1");
        status.ShouldNotBeNull();
        status!.Status.ShouldBe(QuartzJobStatus.InProgress.ToString());
        status.ErrorMessage.ShouldBe("Tenant error");
    }

    [Test]
    public async Task SetStatusAsync_SetsCreatedAt_WhenCreatingNewStatus()
    {
        var beforeCall = DateTime.UtcNow;
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-create-timestamp", QuartzJobStatus.InProgress, null);

        var afterCall = DateTime.UtcNow;
        using var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await context.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-create-timestamp");

        status.ShouldNotBeNull();
        status!.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCall);
        status.CreatedAt.ShouldBeLessThanOrEqualTo(afterCall);
    }

    [Test]
    public async Task SetStatusAsync_KeepsCreatedAt_WhenUpdatingStatus()
    {
        var originalCreatedAt = DateTime.UtcNow.AddHours(-1);
        using (var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()))
        {
            context.JobStatuses.Add(new JobStatus
            {
                JobId = "job-created-at-unchanged",
                Status = QuartzJobStatus.InProgress.ToString(),
                CreatedAt = originalCreatedAt,
                FinishedAt = null
            });
            await context.SaveChangesAsync();
        }

        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-created-at-unchanged", QuartzJobStatus.Completed, null);

        using var verifyContext = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await verifyContext.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-created-at-unchanged");

        status.ShouldNotBeNull();
        status!.CreatedAt.ShouldBe(originalCreatedAt);
    }

    [Test]
    public async Task SetStatusAsync_DoesNotSetFinishedAt_WhenStatusIsPending()
    {
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-pending", QuartzJobStatus.Pending, null);

        using var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await context.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-pending");

        status.ShouldNotBeNull();
        status!.FinishedAt.ShouldBeNull();
    }

    [Test]
    public async Task SetStatusAsync_DoesNotSetFinishedAt_WhenStatusIsInProgress()
    {
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-in-progress", QuartzJobStatus.InProgress, null);

        using var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await context.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-in-progress");

        status.ShouldNotBeNull();
        status!.FinishedAt.ShouldBeNull();
    }

    [Test]
    public async Task SetStatusAsync_SetsFinishedAt_WhenStatusIsCompleted()
    {
        var beforeCall = DateTime.UtcNow;
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-completed", QuartzJobStatus.Completed, null);

        var afterCall = DateTime.UtcNow;
        using var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await context.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-completed");

        status.ShouldNotBeNull();
        status!.FinishedAt.ShouldNotBeNull();
        status.FinishedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeCall);
        status.FinishedAt.Value.ShouldBeLessThanOrEqualTo(afterCall);
    }

    [Test]
    public async Task SetStatusAsync_SetsFinishedAt_WhenStatusIsError()
    {
        var beforeCall = DateTime.UtcNow;
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-error", QuartzJobStatus.Error, null, "Error occurred");

        var afterCall = DateTime.UtcNow;
        using var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await context.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-error");

        status.ShouldNotBeNull();
        status!.FinishedAt.ShouldNotBeNull();
        status.FinishedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeCall);
        status.FinishedAt.Value.ShouldBeLessThanOrEqualTo(afterCall);
    }

    [Test]
    public async Task SetStatusAsync_SetsFinishedAt_WhenTransitioningFromInProgressToCompleted()
    {
        var originalCreatedAt = DateTime.UtcNow.AddMinutes(-5);
        using (var context = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()))
        {
            context.JobStatuses.Add(new JobStatus
            {
                JobId = "job-transition-complete",
                Status = QuartzJobStatus.InProgress.ToString(),
                CreatedAt = originalCreatedAt,
                FinishedAt = null
            });
            await context.SaveChangesAsync();
        }

        var beforeCall = DateTime.UtcNow;
        var service = new JobStatusService(
            new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration()),
            new DummyTenantSpecificDbContextProvider(_dbContextOptions),
            Options.Create(new AppSettings { MultiTenancy = false }));

        await service.SetStatusAsync("job-transition-complete", QuartzJobStatus.Completed, null);

        var afterCall = DateTime.UtcNow;
        using var verifyContext = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await verifyContext.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-transition-complete");

        status.ShouldNotBeNull();
        status!.CreatedAt.ShouldBe(originalCreatedAt);
        status.FinishedAt.ShouldNotBeNull();
        status.FinishedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeCall);
        status.FinishedAt.Value.ShouldBeLessThanOrEqualTo(afterCall);
    }

    // Dummy provider for integration tests
    private class DummyTenantSpecificDbContextProvider(
        DbContextOptions<Infrastructure.AdminApiDbContext> defaultOptions,
        string tenantName = null,
        Infrastructure.AdminApiDbContext tenantDbContext = null) : ITenantSpecificDbContextProvider
    {
        private readonly DbContextOptions<Infrastructure.AdminApiDbContext> _defaultOptions = defaultOptions;
        private readonly string _tenantName = tenantName;
        private readonly Infrastructure.AdminApiDbContext _tenantDbContext = tenantDbContext;

        public Infrastructure.AdminApiDbContext GetAdminApiDbContext(string tenantIdentifier)
        {
            if (_tenantDbContext != null && tenantIdentifier == _tenantName)
                return _tenantDbContext;

            // Fallback to default context for other tenants
            return new Infrastructure.AdminApiDbContext(_defaultOptions, Testing.Configuration());
        }

        public IUsersContext GetUsersContext(string tenantIdentifier)
        {
            throw new System.NotImplementedException();
        }
    }
}
