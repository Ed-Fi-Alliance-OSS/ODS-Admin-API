// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DbInstances;

public class AddDbInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/dbInstances", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(202))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public async static Task<IResult> Handle(Validator validator, AddDbInstanceCommand addDbInstanceCommand, AddDbInstanceRequest request, HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        var added = addDbInstanceCommand.Execute(request);
        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dbinstances/{added.Id}");
        return Results.Accepted(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDbInstanceRequest")]
    public class AddDbInstanceRequest : IAddDbInstanceModel
    {
        [SwaggerSchema(Description = "Name of the database instance", Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = "Database template to use for the instance", Nullable = false)]
        public string? DatabaseTemplate { get; set; }
    }

    public class Validator : AbstractValidator<AddDbInstanceRequest>
    {
        private static readonly string[] _validDatabaseTemplates = Enum.GetNames<SandboxType>();

        public Validator()
        {
            RuleFor(m => m.Name).NotEmpty().MaximumLength(100);
            RuleFor(m => m.DatabaseTemplate).NotEmpty().MaximumLength(100)
                .Must(t => t != null && _validDatabaseTemplates.Contains(t))
                .WithMessage($"'{{PropertyValue}}' is not a valid database template. Allowed values are: {string.Join(", ", _validDatabaseTemplates)}.");
        }
    }
}



