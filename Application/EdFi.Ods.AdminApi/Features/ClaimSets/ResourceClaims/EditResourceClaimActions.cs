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
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ClaimSets.ResourceClaims;

public class EditResourceClaimActions : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/claimSets/{claimSetId}/resourceClaimActions", HandleAddResourceClaims)
       .WithSummaryAndDescription("Adds ResourceClaimAction association to a claim set.", "Add resourceClaimAction association to claim set. At least one action should be enabled. Valid actions are read, create, update, delete, readchanges.\r\nresouceclaimId is required fields.")
       .WithRouteOptions(b => b.WithResponseCode(201))
       .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapPut(endpoints, "/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}", HandleEditResourceClaims)
       .WithSummaryAndDescription("Updates the ResourceClaimActions to a specific resource claim on a claimset.", "Updates  the resourceClaimActions to a  specific resource claim on a claimset. At least one action should be enabled. Valid actions are read, create, update, delete, readchanges.")
       .WithRouteOptions(b => b.WithResponseCode(200))
       .BuildForVersions(AdminApiVersions.V2);
    }

    internal static async Task<IResult> HandleAddResourceClaims(ResourceClaimClaimSetValidator validator,
        EditResourceOnClaimSetCommand editResourcesOnClaimSetCommand,
        UpdateResourcesOnClaimSetCommand updateResourcesOnClaimSetCommand,
        IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IMapper mapper,
        AddResourceClaimOnClaimSetRequest request, int claimSetId)
    {
        request.ClaimSetId = claimSetId;
        await validator.GuardAsync(request);
        await ExecuteHandle(editResourcesOnClaimSetCommand, mapper, request);
        return Results.Ok();
    }

    internal static async Task<IResult> HandleEditResourceClaims(ResourceClaimClaimSetValidator validator,
        EditResourceOnClaimSetCommand editResourcesOnClaimSetCommand,
        UpdateResourcesOnClaimSetCommand updateResourcesOnClaimSetCommand,
        IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IMapper mapper,
        EditResourceClaimOnClaimSetRequest request, int claimSetId, int resourceClaimId)
    {
        request.ClaimSetId = claimSetId;
        request.ResourceClaimId = resourceClaimId;
        await validator.GuardAsync(request);

        await ExecuteHandle(editResourcesOnClaimSetCommand, mapper, request);

        return Results.Ok();
    }

    private static async Task ExecuteHandle(EditResourceOnClaimSetCommand editResourcesOnClaimSetCommand, IMapper mapper, IResourceClaimOnClaimSetRequest request)
    {
        var editResourceOnClaimSetModel = await Task.FromResult(mapper.Map<EditResourceOnClaimSetModel>(request));
        editResourceOnClaimSetModel.ResourceClaim!.Id = request.ResourceClaimId;
        editResourcesOnClaimSetCommand.Execute(editResourceOnClaimSetModel);
    }


    [SwaggerSchema(Title = "AddResourceClaimActionsOnClaimSetRequest")]
    public class AddResourceClaimOnClaimSetRequest : IResourceClaimOnClaimSetRequest
    {
        [SwaggerExclude]
        public int ClaimSetId { get; set; }

        [SwaggerSchema(Description = "ResourceClaim id", Nullable = false)]
        public int ResourceClaimId { get; set; }

        [SwaggerSchema(Nullable = false)]
        public List<ResourceClaimAction>? ResourceClaimActions { get; set; } = new List<ResourceClaimAction>();
    }

    [SwaggerSchema(Title = "EditResourceClaimActionsOnClaimSetRequest")]
    public class EditResourceClaimOnClaimSetRequest : IResourceClaimOnClaimSetRequest
    {
        [SwaggerExclude]
        public int ClaimSetId { get; set; }

        [SwaggerExclude]
        public int ResourceClaimId { get; set; }

        [SwaggerSchema(Nullable = false)]
        public List<ResourceClaimAction> ResourceClaimActions { get; set; } = new List<ResourceClaimAction>();
    }

    public class ResourceClaimClaimSetValidator : AbstractValidator<IResourceClaimOnClaimSetRequest>
    {
        public ResourceClaimClaimSetValidator(IGetClaimSetByIdQuery getClaimSetByIdQuery,
            IGetResourceClaimsAsFlatListQuery getResourceClaimsAsFlatListQuery,
            IGetAllActionsQuery getAllActionsQuery)
        {
            var resourceClaims = getResourceClaimsAsFlatListQuery.Execute();
            var resourceClaimsById = (Lookup<int, ResourceClaim>)resourceClaims
                .ToLookup(rc => rc.Id);

            RuleFor(m => m.ClaimSetId).NotEmpty();

            RuleFor(m => m).Custom((resourceClaimOnClaimSetRequest, context) =>
            {
                ClaimSet claimSet;
                claimSet = getClaimSetByIdQuery.Execute(resourceClaimOnClaimSetRequest.ClaimSetId);
                var actions = getAllActionsQuery.Execute().Select(x => x.ActionName).ToList();
                if (!claimSet.IsEditable)
                {
                    context.AddFailure("ClaimSetId", $"Claim set ({claimSet.Name}) is system reserved. May not be modified.");
                }

                if (resourceClaimOnClaimSetRequest.ResourceClaimActions != null)
                {
                    ResourceClaimValidator.Validate(resourceClaimsById, actions, resourceClaimOnClaimSetRequest, context, claimSet!.Name);
                }
                else
                {
                    context.AddFailure("ResourceClaimActions", FeatureConstants.InvalidResourceClaimActions);
                }
            });
        }

    }


}


