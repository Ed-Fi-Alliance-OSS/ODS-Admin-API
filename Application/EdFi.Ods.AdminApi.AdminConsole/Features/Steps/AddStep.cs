// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Steps;

public class AddStep : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/steps", Execute)
        .WithRouteOptions(b => b.WithResponseCode(201))
        .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public static async Task<IResult> Execute(Validator validator, IAddStepCommand addStepCommand, AddStepRequest request)
    {
        await validator.GuardAsync(request);
        var addedStepResult = await addStepCommand.Execute(request);

        if (addedStepResult != null)
            return Results.Created($"/steps/{addedStepResult.TenantId}/{addedStepResult.DocId}", addedStepResult);
        else
            return Results.BadRequest();
    }

    public class AddStepRequest : IAddStepModel
    {
        [Required]
        public int InstanceId { get; set; }
        public int? EdOrgId { get; set; }
        [Required]
        public int TenantId { get; set; }
        [Required]
        public string Document { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<AddStepRequest>
    {
        public Validator()
        {
            RuleFor(m => m.InstanceId)
             .NotNull();

            RuleFor(m => m.EdOrgId)
             .NotNull();

            RuleFor(m => m.Document)
             .NotNull()
             .NotEmpty()
             .Must(BeValidDocument).WithMessage("Document must be a valid JSON.");
        }

        private static bool BeValidDocument(string document)
        {
            try
            {
                Newtonsoft.Json.Linq.JToken.Parse(document);
                return true;
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return false;
            }
        }
    }
}
