// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using AutoMapper;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Features.Applications;

public class ResetApplicationCredentials : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/applications/{id}/reset-credential", HandleResetCredentials)
            .WithSummary("Reset application credentials. Returns new key and secret.")
            .WithRouteOptions(b => b.WithResponse<ApplicationResult>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> HandleResetCredentials(RegenerateApplicationApiClientSecretCommand resetSecretCommand, IOptions<AppSettings> settings, IMapper mapper, int id)
    {
        if (!settings.Value.EnableApplicationResetEndpoint)
            throw new FluentValidation.ValidationException(new[] { new ValidationFailure(nameof(Application), $"This endpoint has been disabled on application settings.") });

        var resetApplicationSecret = await Task.Run(() => resetSecretCommand.Execute(id));
        var model = mapper.Map<ApplicationResult>(resetApplicationSecret);
        return Results.Ok(model);
    }
}
