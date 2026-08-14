// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Infrastructure.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;
using FluentValidation;
using FluentValidation.Results;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ApiClients;

public class EditApiClient : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/apiClients/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(IEditApiClientCommand editApiClientCommand,
        Validator validator, IUsersContext db, EditApiClientRequest request, int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);
        GuardAgainstInvalidEntityReferences(request, db);
        editApiClientCommand.Execute(request);
        return Results.Ok();
    }

    private static void GuardAgainstInvalidEntityReferences(EditApiClientRequest request, IUsersContext db)
    {
        ValidateOdsInstanceIds(request, db);
    }

    private static void ValidateOdsInstanceIds(EditApiClientRequest request, IUsersContext db)
    {
        var allOdsInstanceIds = new HashSet<int>(db.OdsInstances.Select(p => p.OdsInstanceId));
        EntityReferenceValidator.ValidateIdsExist(request.OdsInstanceIds, allOdsInstanceIds, nameof(request.OdsInstanceIds));
    }

    [SwaggerSchema(Title = "EditApiClientRequest")]
    public class EditApiClientRequest : IEditApiClientModel
    {
        [SwaggerExclude]
        public int Id { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ApiClientNameDescription, Nullable = false)]
        public string Name { get; set; } = string.Empty;

        [SwaggerSchema(Description = FeatureConstants.ApiClientIsApprovedDescription, Nullable = false)]
        public bool IsApproved { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ApiClientApplicationIdDescription, Nullable = false)]
        public int ApplicationId { get; set; }

        [SwaggerSchema(Description = FeatureConstants.OdsInstanceIdsDescription, Nullable = false)]
        public IEnumerable<int>? OdsInstanceIds { get; set; }
    }

    public class Validator : AbstractValidator<IEditApiClientModel>
    {
        public Validator()
        {
            RuleFor(m => m.Id)
                .NotEmpty();

            RuleFor(m => m.Name)
             .NotEmpty();

            RuleFor(m => m.Name)
             .Must(BeWithinApiClientNameMaxLength)
             .WithMessage(FeatureConstants.ApiClientNameLengthValidationMessage)
             .When(x => x.Name != null);

            RuleFor(m => m.ApplicationId)
                .GreaterThan(0);

            RuleFor(m => m.OdsInstanceIds)
                .NotEmpty()
                .WithMessage(FeatureConstants.OdsInstanceIdsValidationMessage);

            static bool BeWithinApiClientNameMaxLength<T>(IEditApiClientModel model, string? name, ValidationContext<T> context)
            {
                var extraCharactersInName = name!.Length - ValidationConstants.MaximumApiClientNameLength;
                if (extraCharactersInName <= 0)
                {
                    return true;
                }
                context.MessageFormatter.AppendArgument("Name", name);
                context.MessageFormatter.AppendArgument("ExtraCharactersInName", extraCharactersInName);
                return false;
            }
        }
    }
}
