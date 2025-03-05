// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Commands;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Permissions;

public class AddPermission : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/permissions", Execute)
      .WithRouteOptions(b => b.WithResponseCode(201))
      .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public async Task<IResult> Execute(Validator validator, IAddPermissionCommand addPermissionCommand, AddPermissionRequest request)
    {
        await validator.GuardAsync(request);
        var addedPermissionResult = await addPermissionCommand.Execute(request);

        if (addedPermissionResult != null)
            return Results.Created($"/permissions/{addedPermissionResult.TenantId}/{addedPermissionResult.DocId}", addedPermissionResult);
        else
        {
            return Results.BadRequest();
        }
    }

    public class AddPermissionRequest : IAddPermissionModel
    {
        [Required]
        public int InstanceId { get; set; }
        public int? EdOrgId { get; set; }
        [Required]
        public int TenantId { get; set; }
        [Required]
        public string Document { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<AddPermissionRequest>
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
             .NotEmpty()
             .Must(BeValidDocument).WithMessage("Document must be a valid JSON.");
        }

        private bool BeValidDocument(string document)
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
