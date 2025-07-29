// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ClaimSets;

public class EditClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/claimSets/{id}", Handle)
        .WithDefaultSummaryAndDescription()
        .WithRouteOptions(b => b.WithResponseCode(200))
        .BuildForVersions(AdminApiVersions.V2);
    }

    public async Task<IResult> Handle(Validator validator, IEditClaimSetCommand editClaimSetCommand,
        UpdateResourcesOnClaimSetCommand updateResourcesOnClaimSetCommand,
        IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications,
        IAuthStrategyResolver strategyResolver,
        IMapper mapper,
        EditClaimSetRequest request, int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);

        var editClaimSetModel = new EditClaimSetModel
        {
            ClaimSetName = request.Name,
            ClaimSetId = id
        };

        try
        {
            editClaimSetCommand.Execute(editClaimSetModel);
        }
        catch (AdminApiException exception)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(id), exception.Message) });
        }
        return Results.Ok();
    }

    [SwaggerSchema(Title = "EditClaimSetRequest")]
    public class EditClaimSetRequest
    {
        [SwaggerExclude]
        public int Id { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ClaimSetNameDescription, Nullable = false)]
        public string? Name { get; set; }
    }

    public class Validator : AbstractValidator<EditClaimSetRequest>
    {
        private readonly IGetClaimSetByIdQuery _getClaimSetByIdQuery;
        private readonly IGetAllClaimSetsQuery _getAllClaimSetsQuery;

        public Validator(IGetClaimSetByIdQuery getClaimSetByIdQuery,
            IGetAllClaimSetsQuery getAllClaimSetsQuery)
        {
            _getClaimSetByIdQuery = getClaimSetByIdQuery;
            _getAllClaimSetsQuery = getAllClaimSetsQuery;

            RuleFor(m => m.Id).NotEmpty();

            RuleFor(m => m.Id)
                .Must(BeAnExistingClaimSet)
                .WithMessage(FeatureConstants.ClaimSetNotFound);

            RuleFor(m => m.Name)
            .NotEmpty()
            .Must(BeAUniqueName)
            .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage)
            .When(m => BeAnExistingClaimSet(m.Id) && NameIsChanged(m));

            RuleFor(m => m.Name)
                .MaximumLength(255)
                .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);
        }

        private bool BeAnExistingClaimSet(int id)
        {
            _getClaimSetByIdQuery.Execute(id);
            return true;
        }

        private bool NameIsChanged(EditClaimSetRequest model)
        {
            return _getClaimSetByIdQuery.Execute(model.Id).Name != model.Name;
        }

        private bool BeAUniqueName(string? name)
        {
            return _getAllClaimSetsQuery.Execute().All(x => x.Name != name);
        }
    }



}
