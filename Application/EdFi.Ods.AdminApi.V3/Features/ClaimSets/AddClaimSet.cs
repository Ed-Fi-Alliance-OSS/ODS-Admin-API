// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.ClaimSets;

public class AddClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/claimSets", Handle)
        .WithDefaultSummaryAndDescription()
        .WithRouteOptions(b => b.WithResponseCode(201))
        .BuildForVersions(AdminApiVersions.V3);
    }

    public async Task<IResult> Handle(Validator validator, AddClaimSetCommand addClaimSetCommand,
        AddOrEditResourcesOnClaimSetCommand addOrEditResourcesOnClaimSetCommand,
        AddClaimSetRequest request,
        HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        var addedClaimSetId = addClaimSetCommand.Execute(new AddClaimSetModel
        {
            ClaimSetName = request.Name ?? string.Empty
        });

        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/claimSets/{addedClaimSetId}");
        return Results.Created(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddClaimSetRequest")]
    public class AddClaimSetRequest
    {
        [SwaggerSchema(Description = FeatureConstants.ClaimSetNameDescription, Nullable = false)]
        public string? Name { get; set; }
    }

    public class Validator : AbstractValidator<AddClaimSetRequest>
    {
        private readonly IGetAllClaimSetsQuery _getAllClaimSetsQuery;

        public Validator(IGetAllClaimSetsQuery getAllClaimSetsQuery)
        {
            _getAllClaimSetsQuery = getAllClaimSetsQuery;

            RuleFor(m => m.Name).NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);

            RuleFor(m => m.Name)
                .MaximumLength(255)
                .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);
        }

        private bool BeAUniqueName(string? name)
        {
            return _getAllClaimSetsQuery.Execute().All(x => x.Name != name);
        }
    }
}



