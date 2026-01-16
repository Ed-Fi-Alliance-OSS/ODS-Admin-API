// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Services.Jobs;

[TestFixture]
public class JobStatusServiceTests : AdminApiDbContextTestBase
{
    private JobStatusService _service = null!;
    private DbContextOptions<Infrastructure.AdminApiDbContext> _dbContextOptions = null!;

    [SetUp]
    public void SetUpService()
    {
        _dbContextOptions = GetAdminApiDbContextOptions(ConnectionString);
        _service = new JobStatusService(new Infrastructure.AdminApiDbContext(
            _dbContextOptions,
            Testing.Configuration()));
    }

    [Test]
    public async Task SetStatusAsync_CreatesNewStatus_WhenNotExists()
    {
        await _service.SetStatusAsync("job-1", QuartzJobStatus.InProgress, "No error");
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
        await _service.SetStatusAsync("job-2", QuartzJobStatus.Completed, "Done");
        using var verifyContext = new Infrastructure.AdminApiDbContext(_dbContextOptions, Testing.Configuration());
        var status = await verifyContext.JobStatuses.FirstOrDefaultAsync(j => j.JobId == "job-2");
        status.ShouldNotBeNull();
        status!.Status.ShouldBe(QuartzJobStatus.Completed.ToString());
        status.ErrorMessage.ShouldBe("Done");
    }
}
