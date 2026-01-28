// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;

public class JobStatusService(AdminApiDbContext dbContext,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    IOptions<AppSettings> options) : IJobStatusService
{
    private readonly ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = tenantSpecificDbContextProvider;
    private readonly bool _isMultiTenancyEnabled = options.Value.MultiTenancy;

    public async Task SetStatusAsync(string jobId, QuartzJobStatus status, string? tenantName, string? errorMessage = null)
    {
        AdminApiDbContext resolvedDbContext;

        if (_isMultiTenancyEnabled && !string.IsNullOrEmpty(tenantName))
        {
            resolvedDbContext = _tenantSpecificDbContextProvider.GetAdminApiDbContext(tenantName);
        }
        else
        {
            resolvedDbContext = dbContext;
        }

        var jobStatus = await resolvedDbContext.JobStatuses
            .FirstOrDefaultAsync(j => j.JobId == jobId);
        if (jobStatus is null)
        {
            jobStatus = new JobStatus
            {
                JobId = jobId,
                Status = status.ToString(),
                ErrorMessage = errorMessage
            };
            resolvedDbContext.JobStatuses.Add(jobStatus);
        }
        else
        {
            jobStatus.Status = status.ToString();
            jobStatus.ErrorMessage = errorMessage;
        }
        await resolvedDbContext.SaveChangesAsync();
    }
}
