// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.WorkerInstances;

public class WorkerInstanceRenameFailed : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/instances/{id}/renameFailed", Handle)
            .WithRouteOptions(b => b.WithResponseCode(204))
            .WithRouteOptions(b => b.WithResponseCode(400))
            .WithRouteOptions(b => b.WithResponseCode(404))
            .BuildForVersions(AuthorizationPolicies.AdminApiWorkerScopePolicy.PolicyName, AdminApiVersions.AdminConsole);
    }

    internal static async Task<IResult> Handle([FromServices] IRenameFailedInstanceCommand renameFailedInstanceCommand, [FromRoute] int id)
    {
        try
        {
            if (id < 1)
                return Results.BadRequest("Instance Id not valid.");

            await renameFailedInstanceCommand.Execute(id);
        }
        catch (AdminApiException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                return Results.Conflict(ex.Message);
            throw;
        }

        return Results.NoContent();
    }
}
