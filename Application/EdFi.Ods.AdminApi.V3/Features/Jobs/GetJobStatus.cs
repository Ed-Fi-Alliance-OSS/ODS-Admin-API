// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Features.Jobs;

public class GetJobStatus : IFeature
{
    public class Response
    {
        public string JobId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapGet(endpoints, "/jobs/{jobId}", Handle)
            .WithSummaryAndDescription(
                "Get job status",
                "Get the status of a job by its ID"
            )
            .WithRouteOptions(b => b
                .WithResponseCode(200)
                .WithResponse<Response>(200)
                .WithResponseCode(404))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal static async Task<IResult> Handle(
        string jobId,
        IJobStatusService jobStatusService,
        [FromServices] IContextProvider<TenantConfiguration> tenantConfigurationProvider,
        [FromServices] IOptions<AppSettings> options,
        HttpContext httpContext)
    {
        string? tenantName = null;
        if (options.Value.MultiTenancy)
        {
            var tenantConfig = tenantConfigurationProvider.Get();
            if (tenantConfig is null)
            {
                var problemDetails = V3ProblemDetailsFactory.Create(
                    status: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Tenant identifier is required when multi-tenancy is enabled.",
                    type: AdminApiProblemTypes.BadRequest,
                    correlationId: httpContext.TraceIdentifier);

                return Results.Json(problemDetails, statusCode: StatusCodes.Status400BadRequest, contentType: "application/problem+json");
            }
            tenantName = tenantConfig.TenantIdentifier;
        }

        var jobStatus = await jobStatusService.GetStatusAsync(jobId, tenantName);

        if (jobStatus is null)
        {
            throw new NotFoundException<string>("Job", jobId);
        }

        var response = new Response
        {
            JobId = jobStatus.JobId,
            Status = jobStatus.Status,
            CreatedAt = jobStatus.CreatedAt,
            FinishedAt = jobStatus.FinishedAt,
            ErrorMessage = jobStatus.ErrorMessage
        };

        return Results.Ok(response);
    }
}
