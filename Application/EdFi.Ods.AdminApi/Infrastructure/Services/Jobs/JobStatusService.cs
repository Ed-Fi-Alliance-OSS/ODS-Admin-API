// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;

public class JobStatusService(AdminApiDbContext dbContext) : IJobStatusService
{
    private readonly AdminApiDbContext _dbContext = dbContext;

    public async Task SetStatusAsync(string jobId, QuartzJobStatus status, string? errorMessage = null)
    {
        var jobStatus = await _dbContext.JobStatuses
            .FirstOrDefaultAsync(j => j.JobId == jobId);
        if (jobStatus is null)
        {
            jobStatus = new JobStatus
            {
                JobId = jobId,
                Status = status.ToString(),
                ErrorMessage = errorMessage
            };
            _dbContext.JobStatuses.Add(jobStatus);
        }
        else
        {
            jobStatus.Status = status.ToString();
            jobStatus.ErrorMessage = errorMessage;
        }
        await _dbContext.SaveChangesAsync();
    }
}
