// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Runtime.CompilerServices;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using log4net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

[assembly: InternalsVisibleTo("EdFi.Ods.AdminApi.UnitTests")]

var builder = WebApplication.CreateBuilder(args);

// Initialize log4net early so we can use it in Program.cs
builder.AddLoggingServices();

// logging
var _logger = LogManager.GetLogger("Program");
_logger.Info("Starting Admin API");
var adminApiMode = builder.Configuration.GetValue<AdminApiMode>("AppSettings:AdminApiMode", AdminApiMode.V2);
var databaseEngine = builder.Configuration.GetValue<string>("AppSettings:DatabaseEngine");

// Log configuration values as requested
_logger.InfoFormat("Configuration - ApiMode: {0}, Engine: {1}", adminApiMode, databaseEngine);

builder.AddServices();

var app = builder.Build();

var pathBase = app.Configuration.GetValue<string>("AppSettings:PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase($"/{pathBase.Trim('/')}");
    app.UseForwardedHeaders();
}

AdminApiVersions.Initialize(app);

//The ordering here is meaningful: Logging -> Routing -> Auth -> Endpoints
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<AdminApiModeValidationMiddleware>();

if (adminApiMode == AdminApiMode.V2 || adminApiMode == AdminApiMode.V3)
    app.UseMiddleware<TenantResolverMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapFeatureEndpoints();

app.MapControllers();
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        // 200 OK if all are healthy, 503 Service Unavailable if any are unhealthy
        context.Response.StatusCode = report.Status == HealthStatus.Unhealthy ? (int)HttpStatusCode.ServiceUnavailable : (int)HttpStatusCode.OK;

        var response = new
        {
            Status = report.Status.ToString(),
            Results = report.Entries.GroupBy(x => x.Value.Tags.FirstOrDefault()).Select(x => new
            {
                Name = x.Key,
                Status = x.Min(y => y.Value.Status).ToString()
            })
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

if (app.Configuration.GetValue<bool>("SwaggerSettings:EnableSwagger"))
{
    app.UseSwagger();
    app.DefineSwaggerUIWithApiVersions(AdminApiVersions.GetAllVersionStrings());
}

var edOrgsRefreshIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:EdOrgsRefreshIntervalInMins"
);
var createDbInstancesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:CreateDbInstancesSweepIntervalInMins"
);
var isMultiTenancyEnabled = app.Configuration.GetValue<bool>(
    "AppSettings:MultiTenancy"
);

if (adminApiMode == AdminApiMode.V2)
{
    var shouldScheduleDispatcher = double.TryParse(createDbInstancesSweepIntervalInMins, out var createDbInstancesSweepInterval);
    var shouldScheduleEdOrgsRefresh = double.TryParse(edOrgsRefreshIntervalInMins, out var refreshInterval);

    if (isMultiTenancyEnabled && (shouldScheduleDispatcher || shouldScheduleEdOrgsRefresh))
    {
        using var scope = app.Services.CreateScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantsService>();
        await tenantService.InitializeTenantsAsync();
    }

    var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();

    if (shouldScheduleEdOrgsRefresh)
    {
        if (isMultiTenancyEnabled)
        {
            using var scope = app.Services.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantsService>();
            var tenants = await tenantService.GetTenantsAsync(fromCache: true);

            foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
            {
                await QuartzJobScheduler.ScheduleJob<RefreshEducationOrganizationsJob>(
                    scheduler,
                    jobKey: new JobKey($"{JobConstants.RefreshEducationOrganizationsJobName}_{tenantName}"),
                    jobData: new Dictionary<string, object>
                    {
                        [JobConstants.TenantNameKey] = tenantName
                    },
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(refreshInterval)
                );
            }
        }
        else
        {
            await QuartzJobScheduler.ScheduleJob<RefreshEducationOrganizationsJob>(
                scheduler,
                jobKey: new JobKey(JobConstants.RefreshEducationOrganizationsJobName),
                jobData: new Dictionary<string, object>(),
                startImmediately: false,
                interval: TimeSpan.FromMinutes(refreshInterval)
            );
        }

    }
    else
    {
        _logger.Error("Invalid value for EdOrgsRefreshIntervalInMins. Please ensure it is a valid number.");
    }

    if (shouldScheduleDispatcher)
    {
        if (isMultiTenancyEnabled)
        {
            using var scope = app.Services.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantsService>();
            var tenants = await tenantService.GetTenantsAsync(fromCache: true);

            foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
            {
                await QuartzJobScheduler.ScheduleJob<CreatePendingDbInstancesDispatcherJob>(
                    scheduler,
                    jobKey: new JobKey($"{JobConstants.CreatePendingDbInstancesDispatcherJobName}_{tenantName}"),
                    jobData: new Dictionary<string, object>
                    {
                        [JobConstants.TenantNameKey] = tenantName
                    },
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(createDbInstancesSweepInterval)
                );
            }
        }
        else
        {
            await QuartzJobScheduler.ScheduleJob<CreatePendingDbInstancesDispatcherJob>(
                scheduler,
                jobKey: new JobKey(JobConstants.CreatePendingDbInstancesDispatcherJobName),
                jobData: new Dictionary<string, object>(),
                startImmediately: false,
                interval: TimeSpan.FromMinutes(createDbInstancesSweepInterval)
            );
        }
    }
    else
    {
        _logger.Error("Invalid value for CreateDbInstancesSweepIntervalInMins. Please ensure it is a valid number.");
    }
}

await app.RunAsync();
