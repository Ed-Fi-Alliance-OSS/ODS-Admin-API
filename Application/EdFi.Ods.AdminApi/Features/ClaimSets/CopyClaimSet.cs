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
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApp.Management.ClaimSetEditor;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ClaimSets;

public class CopyClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/claimSets/copy", Handle)
        .WithSummary("Copies the existing claimset and create a new one.")
        .WithRouteOptions(b => b.WithResponseCode(201))
        .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(Validator validator, ICopyClaimSetCommand copyClaimSetCommand,
        IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications,
        IMapper mapper,
        CopyClaimSetRequest request)
    {
        await validator.GuardAsync(request);
        var copiedClaimSetId = copyClaimSetCommand.Execute(request);

        return Results.Created($"/claimSets/{copiedClaimSetId}", null);
    }

    [SwaggerSchema(Title = "CopyClaimSetRequest")]
    public class CopyClaimSetRequest : ICopyClaimSetModel
    {
        [SwaggerSchema(Description = FeatureConstants.ClaimSetIdToCopy, Nullable = false)]
        public int OriginalId { get; set; }

        [SwaggerSchema(Description = "New claimset name", Nullable = false)]
        public string? Name { get; set; }
    }

    public class Validator : AbstractValidator<CopyClaimSetRequest>
    {
        private readonly IGetAllClaimSetsQuery _getAllClaimSetsQuery;
        private readonly IGetClaimSetByIdQuery _getClaimSetByIdQuery;

        public Validator(IGetAllClaimSetsQuery getAllClaimSetsQuery,
            IGetClaimSetByIdQuery getClaimSetByIdQuery)
        {
            _getAllClaimSetsQuery = getAllClaimSetsQuery;
            _getClaimSetByIdQuery = getClaimSetByIdQuery;

            RuleFor(m => m.OriginalId)
              .Must(BeAnExistingClaimSet)
              .WithMessage("No such claim set exists in the database.Please provide valid claimset id.");

            RuleFor(m => m.Name).NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);

            RuleFor(m => m.Name)
                .MaximumLength(255)
                .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);
        }

        private bool BeAnExistingClaimSet(int id)
        {
            try
            {
                _getClaimSetByIdQuery.Execute(id);
                return true;
            }
            catch (AdminApiException)
            {
                throw new NotFoundException<int>("claimSet", id);
            }
        }

        private bool BeAUniqueName(string? name)
        {
            return _getAllClaimSetsQuery.Execute().All(x => !string.IsNullOrEmpty(x.Name)
                          && !x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
