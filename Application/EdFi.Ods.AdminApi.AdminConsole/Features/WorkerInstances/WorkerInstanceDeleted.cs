// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoMapper;
using EdFi.Ods.AdminApi.AdminConsole.Features.Instances;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.WorkerInstances;

public class WorkerInstanceDeleted : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/instances/{id}/deleted", Handle)
            .WithRouteOptions(b => b.WithResponseCode(204))
            .WithRouteOptions(b => b.WithResponseCode(400))
            .WithRouteOptions(b => b.WithResponseCode(404))
            .BuildForVersions(AdminApiVersions.AdminConsole);
        AdminApiEndpointBuilder.MapPost(endpoints, "/instances/{id}/deletefailed", HandleDeleteFailed)
            .WithRouteOptions(b => b.WithResponseCode(204))
            .WithRouteOptions(b => b.WithResponseCode(400))
            .WithRouteOptions(b => b.WithResponseCode(404))
            .WithRouteOptions(b => b.WithResponseCode(409 ))
            .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    internal static async Task<IResult> Handle([FromServices] IDeletedInstanceCommand deletedInstanceCommand, [FromRoute] int id)
    {
        try
        {
            if (id < 1)
                return Results.BadRequest("Instance Id not valid.");

            await deletedInstanceCommand.Execute(id);
            return Results.NoContent();
        }
        catch (NotFoundException<int> ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    internal static async Task<IResult> HandleDeleteFailed([FromServices] IDeleteInstanceFailedCommand deletedInstanceCommand, [FromRoute] int id)
    {
        try
        {
            if (id < 1)
                return Results.BadRequest("Instance Id not valid.");

            await deletedInstanceCommand.Execute(id);
            return Results.NoContent();
        }
        catch (NotFoundException<int> ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (AdminApiException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Conflict)
                return Results.Conflict(ex.Message);
            throw;
        }
    }
}
