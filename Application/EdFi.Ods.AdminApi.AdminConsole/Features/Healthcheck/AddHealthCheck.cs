// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.HealthChecks.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Healthcheck;
public class AddHealthCheck : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/healthcheck", Execute)
      .WithRouteOptions(b => b.WithResponseCode(201))
      .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public static async Task<IResult> Execute(Validator validator, IAddHealthCheckCommand addHealthCheckCommand, AddHealthCheckRequest request)
    {
        await validator.GuardAsync(request);
        var addedHealthCheck = await addHealthCheckCommand.Execute(request);
        return Results.Created($"/healthcheck/{addedHealthCheck.DocId}", null);
    }

    [SwaggerSchema(Title = nameof(AddHealthCheckRequest))]
    public class AddHealthCheckRequest : IAddHealthCheckModel
    {
        [Required]
        public int DocId { get; set; }
        [Required]
        public int InstanceId { get; set; }
        [Required]
        public int EdOrgId { get; set; }
        [Required]
        public int TenantId { get; set; }
        [Required]
        public string Document { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<AddHealthCheckRequest>
    {
        public Validator()
        {
            RuleFor(m => m.InstanceId)
                 .NotNull();

            RuleFor(m => m.EdOrgId)
                 .NotNull();

            RuleFor(m => m.TenantId)
                 .NotNull();

            RuleFor(m => m.Document)
                 .NotNull()
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
