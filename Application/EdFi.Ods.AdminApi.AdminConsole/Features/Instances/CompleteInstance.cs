// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

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

    public static async Task<IResult> Handle(Validator validator, ICompleteInstanceCommand completeInstanceCommand, int instanceid, [FromBody] CompleteInstanceRequest request)
    {
        if (instanceid < 1)
            return Results.BadRequest("Instance Id not valid.");

        await validator.GuardAsync(request);

        await completeInstanceCommand.Execute(instanceid, request.ConnectionString);
        return Results.NoContent();
    }

    public class CompleteInstanceRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<CompleteInstanceRequest>
    {
        private readonly string _databaseEngine;
        public Validator(IOptions<AppSettings> options)
        {
            _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .WithMessage("Connection string is required.");

            RuleFor(x => x.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage("Connection string is not valid. Please check the connection string and try again.");
        }

        private bool BeAValidConnectionString(string? connectionString)
        {
            return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
        }
    }
}
