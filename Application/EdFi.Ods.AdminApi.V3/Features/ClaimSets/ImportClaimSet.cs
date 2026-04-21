// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.ClaimSets;

public class ImportClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/claimSets/import", Handle)
            .WithSummary("Imports a new claimset")
            .WithRouteOptions(b => b.WithResponseCode(201))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal async Task<IResult> Handle(Validator validator, AddClaimSetCommand addClaimSetCommand,
        AddOrEditResourcesOnClaimSetCommand addOrEditResourcesOnClaimSetCommand,
        IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications,
        IAuthStrategyResolver strategyResolver,
        ImportClaimSetRequest request)
    {
        await validator.GuardAsync(request);
        var addedClaimSetId = addClaimSetCommand.Execute(new AddClaimSetModel
        {
            ClaimSetName = request.Name ?? string.Empty
        });

        var resourceClaims = ClaimSetMapper.ToResourceClaimList(request.ResourceClaims ?? []);
        var resolvedResourceClaims = strategyResolver.ResolveAuthStrategies(resourceClaims).ToList();

        addOrEditResourcesOnClaimSetCommand.Execute(addedClaimSetId, resolvedResourceClaims);

        var claimSet = getClaimSetByIdQuery.Execute(addedClaimSetId);

        return Results.Created($"/claimSets/{claimSet.Id}", null);
    }

    [SwaggerSchema(Title = "ImportClaimSetRequest")]
    public class ImportClaimSetRequest
    {
        [SwaggerSchema(Description = FeatureConstants.ClaimSetNameDescription, Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ResourceClaimsDescription, Nullable = false)]
        public List<ClaimSetResourceClaimModel>? ResourceClaims { get; set; }
    }

    public class Validator : AbstractValidator<ImportClaimSetRequest>
    {
        private readonly IGetAllClaimSetsQuery _getAllClaimSetsQuery;

        public Validator(IGetAllClaimSetsQuery getAllClaimSetsQuery,
            IGetResourceClaimsAsFlatListQuery getResourceClaimsAsFlatListQuery,
            IGetAllAuthorizationStrategiesQuery getAllAuthorizationStrategiesQuery,
            IGetAllActionsQuery getAllActionsQuery)
        {
            _getAllClaimSetsQuery = getAllClaimSetsQuery;

            var resourceClaims = (Lookup<string, ResourceClaim>)getResourceClaimsAsFlatListQuery.Execute()
                .ToLookup(rc => rc.Name?.ToLower());

            var authStrategyNames = getAllAuthorizationStrategiesQuery.Execute()
                .Select(a => a.AuthStrategyName).ToList();

            RuleFor(m => m.Name).NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage);

            RuleFor(m => m.Name)
                .MaximumLength(255)
                .WithMessage(FeatureConstants.ClaimSetNameMaxLengthMessage);

            RuleFor(m => m).Custom((claimSet, context) =>
            {
                var resourceClaimValidator = new ResourceClaimValidator();
                var actions = getAllActionsQuery.Execute().Select(x => x.ActionName).ToList();

                if (claimSet.ResourceClaims != null && claimSet.ResourceClaims.Any())
                {
                    foreach (var resourceClaim in claimSet.ResourceClaims)
                    {
                        resourceClaimValidator.Validate(resourceClaims, actions, authStrategyNames,
                            resourceClaim, claimSet.ResourceClaims.ToList(), context, claimSet.Name);
                    }
                }
            });
        }

        private bool BeAUniqueName(string? name)
        {
            return _getAllClaimSetsQuery.Execute().All(x => x.Name != name);
        }
    }
}



