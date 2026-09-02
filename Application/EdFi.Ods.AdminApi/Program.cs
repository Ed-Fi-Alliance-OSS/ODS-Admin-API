// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Runtime.CompilerServices;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using V3Jobs = EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using V3Tenants = EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using V3ErrorHandling = EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using V3Features = EdFi.Ods.AdminApi.V3.Features;
using log4net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
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
}

var reverseProxySettings = app.Services.GetRequiredService<IOptions<ReverseProxySettings>>().Value;
if (reverseProxySettings.UseForwardedHeaders)
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions();
    ForwardedHeadersConfigurator.Configure(forwardedHeadersOptions, reverseProxySettings);
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

AdminApiVersions.Initialize(app);

//The ordering here is meaningful: Audit -> Logging -> Routing -> Auth -> Endpoints
//AuditActionLoggingMiddleware must be outermost so it observes the final response status
//code after RequestLoggingMiddleware/V3RequestErrorMiddleware has translated any exception
//into its real HTTP status (they catch and never rethrow), rather than guessing 500 itself.
app.UseMiddleware<AuditActionLoggingMiddleware>();

if (adminApiMode == AdminApiMode.V3)
{
    app.UseMiddleware<V3ErrorHandling.V3RequestErrorMiddleware>();
    app.UseMiddleware<V3Features.AdminApiModeValidationMiddleware>();
}
else
{
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<AdminApiModeValidationMiddleware>();
}

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

// The raw OpenAPI/swagger.json documents are always served so that the /information endpoint's
// openApiMetadata URL is always reachable; only the interactive Swagger UI is flag-gated.
app.UseSwagger();
if (app.Configuration.GetValue<bool>("SwaggerSettings:EnableSwagger"))
{
    var currentVersion = adminApiMode switch
    {
        AdminApiMode.V1 => AdminApiVersions.V1,
        AdminApiMode.V2 => AdminApiVersions.V2,
        AdminApiMode.V3 => AdminApiVersions.V3,
        _ => throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}")
    };
    app.DefineSwaggerUIWithApiVersions(currentVersion.ToString());
}

var edOrgsRefreshIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:EdOrgsRefreshIntervalInMins"
);
var createOdsInstanceManagesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:CreateOdsInstanceManagesSweepIntervalInMins"
);
var deleteOdsInstanceManagesSweepIntervalInMins = app.Configuration.GetValue<string>(
    "AppSettings:DeleteOdsInstanceManagesSweepIntervalInMins"
);
var isMultiTenancyEnabled = app.Configuration.GetValue<bool>(
    "AppSettings:MultiTenancy"
);
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;

if (adminApiMode == AdminApiMode.V2)
{
    var shouldScheduleDispatcher = double.TryParse(createOdsInstanceManagesSweepIntervalInMins, out var createOdsInstanceManagesSweepInterval);
    var shouldScheduleDeleteDispatcher = double.TryParse(deleteOdsInstanceManagesSweepIntervalInMins, out var deleteOdsInstanceManagesSweepInterval);
    var shouldScheduleEdOrgsRefresh = double.TryParse(edOrgsRefreshIntervalInMins, out var refreshInterval);

    if (isMultiTenancyEnabled && (shouldScheduleDispatcher || shouldScheduleDeleteDispatcher || shouldScheduleEdOrgsRefresh))
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
        if (DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(appSettings))
        {
            if (isMultiTenancyEnabled)
            {
                using var scope = app.Services.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantsService>();
                var tenants = await tenantService.GetTenantsAsync(fromCache: true);

                foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
                {
                    await QuartzJobScheduler.ScheduleJob<CreatePendingOdsInstanceManagesDispatcherJob>(
                        scheduler,
                        jobKey: new JobKey($"{JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName}_{tenantName}"),
                        jobData: new Dictionary<string, object>
                        {
                            [JobConstants.TenantNameKey] = tenantName
                        },
                        startImmediately: false,
                        interval: TimeSpan.FromMinutes(createOdsInstanceManagesSweepInterval)
                    );
                }
            }
            else
            {
                await QuartzJobScheduler.ScheduleJob<CreatePendingOdsInstanceManagesDispatcherJob>(
                    scheduler,
                    jobKey: new JobKey(JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName),
                    jobData: new Dictionary<string, object>(),
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(createOdsInstanceManagesSweepInterval)
                );
            }
        }
        else
        {
            _logger.Info("EnableDataStoreManagement is false; skipping CreatePendingOdsInstanceManagesDispatcherJob scheduling.");
        }
    }
    else
    {
        _logger.Error("Invalid value for CreateOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
    }

    if (shouldScheduleDeleteDispatcher)
    {
        if (DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(appSettings))
        {
            if (isMultiTenancyEnabled)
            {
                using var scope = app.Services.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantsService>();
                var tenants = await tenantService.GetTenantsAsync(fromCache: true);

                foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
                {
                    await QuartzJobScheduler.ScheduleJob<DeletePendingOdsInstanceManagesDispatcherJob>(
                        scheduler,
                        jobKey: new JobKey($"{JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName}_{tenantName}"),
                        jobData: new Dictionary<string, object>
                        {
                            [JobConstants.TenantNameKey] = tenantName
                        },
                        startImmediately: false,
                        interval: TimeSpan.FromMinutes(deleteOdsInstanceManagesSweepInterval)
                    );
                }
            }
            else
            {
                await QuartzJobScheduler.ScheduleJob<DeletePendingOdsInstanceManagesDispatcherJob>(
                    scheduler,
                    jobKey: new JobKey(JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName),
                    jobData: new Dictionary<string, object>(),
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(deleteOdsInstanceManagesSweepInterval)
                );
            }
        }
        else
        {
            _logger.Info("EnableDataStoreManagement is false; skipping DeletePendingOdsInstanceManagesDispatcherJob scheduling.");
        }
    }
    else
    {
        _logger.Error("Invalid value for DeleteOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
    }
}
else if (adminApiMode == AdminApiMode.V3)
{
    var shouldScheduleDispatcher = double.TryParse(createOdsInstanceManagesSweepIntervalInMins, out var createOdsInstanceManagesSweepInterval) && createOdsInstanceManagesSweepInterval > 0;
    var shouldScheduleDeleteDispatcher = double.TryParse(deleteOdsInstanceManagesSweepIntervalInMins, out var deleteOdsInstanceManagesSweepInterval) && deleteOdsInstanceManagesSweepInterval > 0;
    var shouldScheduleEdOrgsRefresh = double.TryParse(edOrgsRefreshIntervalInMins, out var refreshInterval) && refreshInterval > 0;

    if (isMultiTenancyEnabled && (shouldScheduleDispatcher || shouldScheduleDeleteDispatcher || shouldScheduleEdOrgsRefresh))
    {
        using var scope = app.Services.CreateScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<V3Tenants.ITenantsService>();
        await tenantService.InitializeTenantsAsync();
    }

    var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();

    if (shouldScheduleEdOrgsRefresh)
    {
        if (isMultiTenancyEnabled)
        {
            using var scope = app.Services.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<V3Tenants.ITenantsService>();
            var tenants = await tenantService.GetTenantsAsync(fromCache: true);

            foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
            {
                await QuartzJobScheduler.ScheduleJob<V3Jobs.RefreshEducationOrganizationsJob>(
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
            await QuartzJobScheduler.ScheduleJob<V3Jobs.RefreshEducationOrganizationsJob>(
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
        if (DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(appSettings))
        {
            if (isMultiTenancyEnabled)
            {
                using var scope = app.Services.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<V3Tenants.ITenantsService>();
                var tenants = await tenantService.GetTenantsAsync(fromCache: true);

                foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
                {
                    await QuartzJobScheduler.ScheduleJob<V3Jobs.CreatePendingDataStoreManagesDispatcherJob>(
                        scheduler,
                        jobKey: new JobKey($"{JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName}_{tenantName}"),
                        jobData: new Dictionary<string, object>
                        {
                            [JobConstants.TenantNameKey] = tenantName
                        },
                        startImmediately: false,
                        interval: TimeSpan.FromMinutes(createOdsInstanceManagesSweepInterval)
                    );
                }
            }
            else
            {
                await QuartzJobScheduler.ScheduleJob<V3Jobs.CreatePendingDataStoreManagesDispatcherJob>(
                    scheduler,
                    jobKey: new JobKey(JobConstants.CreatePendingOdsInstanceManagesDispatcherJobName),
                    jobData: new Dictionary<string, object>(),
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(createOdsInstanceManagesSweepInterval)
                );
            }
        }
        else
        {
            _logger.Info("EnableDataStoreManagement is false; skipping CreatePendingDataStoreManagesDispatcherJob scheduling.");
        }
    }
    else
    {
        _logger.Error("Invalid value for CreateOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
    }

    if (shouldScheduleDeleteDispatcher)
    {
        if (DataStoreManagementJobScheduler.ShouldScheduleDataStoreManagementJobs(appSettings))
        {
            if (isMultiTenancyEnabled)
            {
                using var scope = app.Services.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<V3Tenants.ITenantsService>();
                var tenants = await tenantService.GetTenantsAsync(fromCache: true);

                foreach (var tenantName in tenants.Select(tenant => tenant.TenantName))
                {
                    await QuartzJobScheduler.ScheduleJob<V3Jobs.DeletePendingDataStoreManagesDispatcherJob>(
                        scheduler,
                        jobKey: new JobKey($"{JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName}_{tenantName}"),
                        jobData: new Dictionary<string, object>
                        {
                            [JobConstants.TenantNameKey] = tenantName
                        },
                        startImmediately: false,
                        interval: TimeSpan.FromMinutes(deleteOdsInstanceManagesSweepInterval)
                    );
                }
            }
            else
            {
                await QuartzJobScheduler.ScheduleJob<V3Jobs.DeletePendingDataStoreManagesDispatcherJob>(
                    scheduler,
                    jobKey: new JobKey(JobConstants.DeletePendingOdsInstanceManagesDispatcherJobName),
                    jobData: new Dictionary<string, object>(),
                    startImmediately: false,
                    interval: TimeSpan.FromMinutes(deleteOdsInstanceManagesSweepInterval)
                );
            }
        }
        else
        {
            _logger.Info("EnableDataStoreManagement is false; skipping DeletePendingDataStoreManagesDispatcherJob scheduling.");
        }
    }
    else
    {
        _logger.Error("Invalid value for DeleteOdsInstanceManagesSweepIntervalInMins. Please ensure it is a valid number.");
    }
}

await app.RunAsync();
