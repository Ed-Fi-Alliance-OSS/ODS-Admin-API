// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V1.Infrastructure.JsonContractResolvers;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using FluentValidation;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Features.ClaimSets;

public class AddClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/claimsets", Handle)
        .WithDefaultSummaryAndDescription()
        .WithRouteOptions(b => b.WithResponse<ClaimSetDetailsModel>(201))
        .BuildForVersions(AdminApiVersions.V1);
    }

    public async Task<IResult> Handle(Validator validator, AddClaimSetCommand addClaimSetCommand,
        AddOrEditResourcesOnClaimSetCommand addOrEditResourcesOnClaimSetCommand,
        IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetApplicationsByClaimSetIdQuery getApplications,
        IAuthStrategyResolver strategyResolver,
        IMapper mapper,
        Request request)
    {
        await validator.GuardAsync(request);
        var addedClaimSetId = addClaimSetCommand.Execute(new AddClaimSetModel
        {
            ClaimSetName = request.Name ?? string.Empty
        });

        var resourceClaims = mapper.Map<List<ResourceClaim>>(request.ResourceClaims);

        var resolvedResourceClaims = strategyResolver.ResolveAuthStrategies(resourceClaims).ToList();

        addOrEditResourcesOnClaimSetCommand.Execute(addedClaimSetId, resolvedResourceClaims);

        var claimSet = getClaimSetByIdQuery.Execute(addedClaimSetId);

        var model = mapper.Map<ClaimSetDetailsModel>(claimSet);
        model.ApplicationsCount = getApplications.ExecuteCount(addedClaimSetId);
        model.ResourceClaims = getResourcesByClaimSetIdQuery.AllResources(addedClaimSetId)
            .Select(r => mapper.Map<ResourceClaimModel>(r)).ToList();


        return AdminApiResponse<ClaimSetDetailsModel>.Created(
            model,
            "ClaimSet",
            $"/claimsets/{addedClaimSetId}",
            new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new ShouldSerializeContractResolver()
            });
    }

    [SwaggerSchema(Title = "AddClaimSetRequest")]
    public class Request
    {
        [SwaggerSchema(Description = FeatureConstants.ClaimSetNameDescription, Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ResourceClaimsDescription, Nullable = false)]
        public List<RequestResourceClaimModel>? ResourceClaims { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        private readonly IGetAllClaimSetsQuery _getAllClaimSetsQuery;

        public Validator(IGetAllClaimSetsQuery getAllClaimSetsQuery,
            IGetResourceClaimsAsFlatListQuery getResourceClaimsAsFlatListQuery,
            IGetAllAuthorizationStrategiesQuery getAllAuthorizationStrategiesQuery,
            IMapper mapper)
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

                if (claimSet.ResourceClaims != null && claimSet.ResourceClaims.Count != 0)
                {
                    foreach (var resourceClaim in claimSet.ResourceClaims)
                    {
                        resourceClaimValidator.Validate(resourceClaims, authStrategyNames,
                            resourceClaim, mapper.Map<List<ChildrenRequestResourceClaimModel>>(claimSet.ResourceClaims), context, claimSet.Name);
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
