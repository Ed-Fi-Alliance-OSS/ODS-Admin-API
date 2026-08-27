// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.V3.Infrastructure.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;
using FluentValidation;
using FluentValidation.Results;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.ApiClients;

public class EditApiClient : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/apiClients/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(IEditApiClientCommand editApiClientCommand,
        Validator validator, IUsersContext db, EditApiClientRequest request, int id)
    {
        ValidatorExtensions.GuardRouteIdMatchesBodyId(id, request.Id, nameof(request.Id));
        request.Id = id;
        await validator.GuardAsync(request);
        GuardAgainstInvalidEntityReferences(request, db);
        editApiClientCommand.Execute(request);
        return Results.NoContent();
    }

    private static void GuardAgainstInvalidEntityReferences(EditApiClientRequest request, IUsersContext db)
    {
        ValidateDataStoreIds(request, db);
    }

    private static void ValidateDataStoreIds(EditApiClientRequest request, IUsersContext db)
    {
        var allDataStoreIds = new HashSet<int>(db.OdsInstances.Select(p => p.OdsInstanceId));
        EntityReferenceValidator.ValidateIdsExist(request.DataStoreIds, allDataStoreIds, nameof(request.DataStoreIds));
    }

    [SwaggerSchema(Title = "EditApiClientRequest")]
    public class EditApiClientRequest : IEditApiClientModel
    {
        [SwaggerSchema(Description = FeatureConstants.ApiClientIdDescription, Nullable = false)]
        public int Id { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ApiClientNameDescription, Nullable = false)]
        public string Name { get; set; } = string.Empty;

        [SwaggerSchema(Description = FeatureConstants.ApiClientIsApprovedDescription, Nullable = false)]
        public bool IsApproved { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ApiClientApplicationIdDescription, Nullable = false)]
        public int ApplicationId { get; set; }

        [SwaggerSchema(Description = FeatureConstants.DataStoreIdsDescription, Nullable = false)]
        public IEnumerable<int>? DataStoreIds { get; set; }
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

            RuleFor(m => m.DataStoreIds)
                .NotEmpty()
                .WithMessage(FeatureConstants.DataStoreIdsValidationMessage);

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




