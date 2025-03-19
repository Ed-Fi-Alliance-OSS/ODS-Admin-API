// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Instances;
public class DeleteInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/odsInstances/{id}", Execute)
        .WithRouteOptions(b => b.WithResponseCode(202))
        .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    private static async Task<IResult> Execute(int id, IPendingDeleteInstanceCommand changeStatusInstanceCommand)
    {
        try
        {
            await changeStatusInstanceCommand.Execute(id);
        }
        catch (NotFoundException<int> ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (ValidationException ex)
        {
            return Results.Conflict(ex.Errors.Count() == 1 ? ex.Errors.First().ErrorMessage : ex.Message);
        }

        return Results.StatusCode(StatusCodes.Status202Accepted);
    }
}
