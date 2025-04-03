// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Instances;

public class CompleteInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/instances/{instanceid}/completed", Handle)
            .WithRouteOptions(b => b.WithResponseCode(204))
            .WithRouteOptions(b => b.WithResponseCode(400))
            .WithRouteOptions(b => b.WithResponseCode(404))
            .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public static async Task<IResult> Handle(ICompleteInstanceCommand completeInstanceCommand, int instanceid)
    {
        if (instanceid < 1)
            return Results.BadRequest("Instance Id not valid.");
        await completeInstanceCommand.Execute(instanceid);
        return Results.NoContent();
    }
}
