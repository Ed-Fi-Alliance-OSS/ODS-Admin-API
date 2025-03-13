// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Queries;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Steps;

public class ReadSteps : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/steps", GetSteps)
           .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/steps/{tenantId}/{id}", GetStepById)
           .WithRouteOptions(b => b.WithResponse<StepModel>(200))
           .BuildForVersions(AdminApiVersions.AdminConsole);

        AdminApiEndpointBuilder.MapGet(endpoints, "/steps/{tenantId}", GetStepsByTenantId)
           .WithRouteOptions(b => b.WithResponse<StepModel>(200))
           .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    internal static async Task<IResult> GetSteps([FromServices] IGetStepsQuery getStepsQuery)
    {
        var steps = await getStepsQuery.Execute();
        return Results.Ok(steps);
    }

    internal static async Task<IResult> GetStepById([FromServices] IGetStepsByIdQuery GetStepsByIdQuery, int tenantId, int id)
    {
        var step = await GetStepsByIdQuery.Execute(tenantId, id);

        if (step != null)
        {
            return Results.Ok(step);
        }
        return Results.NotFound();
    }

    internal static async Task<IResult> GetStepsByTenantId([FromServices] IGetStepsByTenantIdQuery GetStepsByTenantIdQuery, int tenantId)
    {
        var steps = await GetStepsByTenantIdQuery.Execute(tenantId);

        if (steps.Any())
        {
            return Results.Ok(steps);
        }
        return Results.NotFound();
    }
}
