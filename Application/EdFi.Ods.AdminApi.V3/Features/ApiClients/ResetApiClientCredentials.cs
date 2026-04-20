// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.V3.Features.ApiClients;

public class ResetApiClientCredentials : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/apiclients/{id}/reset-credential", HandleResetCredentials)
            .WithSummary("Reset apiclient credentials. Returns new key and secret.")
            .WithRouteOptions(b => b.WithResponse<ApiClientResult>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> HandleResetCredentials(IRegenerateApiClientSecretCommand resetSecretCommand, int id)
    {
        var resetCredentials = await Task.Run(() => resetSecretCommand.Execute(id));
        var model = ApiClientMapper.ToResult(resetCredentials);
        return Results.Ok(model);
    }
}



