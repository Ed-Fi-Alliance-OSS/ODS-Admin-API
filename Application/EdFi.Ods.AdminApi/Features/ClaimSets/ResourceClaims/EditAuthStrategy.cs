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
using FluentValidation;
using FluentValidation.Results;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.ClaimSets.ResourceClaims;

public class EditAuthStrategy : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/overrideAuthorizationStrategy", HandleOverrideAuthStrategies)
       .WithSummaryAndDescription("Overrides the default authorization strategies on provided resource claim for a specific action.", "Override the default authorization strategies on provided resource claim for a specific action.\r\n\r\nex: actionName = read,  authorizationStrategies= [ \"Ownershipbased\" ]")
       .WithRouteOptions(b => b.WithResponseCode(200))
       .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapPost(endpoints, "/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/resetAuthorizationStrategies", HandleResetAuthStrategies)
        .WithSummary("Resets to default authorization strategies on provided resource claim.")
        .WithRouteOptions(b => b.WithResponseCode(200))
        .BuildForVersions(AdminApiVersions.V2);
    }

    internal static async Task<IResult> HandleOverrideAuthStrategies(OverrideAuthStategyOnClaimSetValidator validator,
      OverrideDefaultAuthorizationStrategyCommand overrideDefaultAuthorizationStrategyCommand, IMapper mapper,
      OverrideAuthStategyOnClaimSetRequest request, int claimSetId, int resourceClaimId)
    {
        request.ClaimSetId = claimSetId;
        request.ResourceClaimId = resourceClaimId;
        await validator.GuardAsync(request);
        var model = mapper.Map<OverrideAuthStrategyOnClaimSetModel>(request);
        overrideDefaultAuthorizationStrategyCommand.ExecuteOnSpecificAction(model);
        return Results.Ok();
    }

    internal async Task<IResult> HandleResetAuthStrategies(IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        OverrideDefaultAuthorizationStrategyCommand overrideDefaultAuthorizationStrategyCommand, IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IMapper mapper, int claimSetId, int resourceClaimId)
    {
        var claimSet = getClaimSetByIdQuery.Execute(claimSetId);

        if (!claimSet.IsEditable)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(claimSetId), $"Claim set ({claimSet.Name}) is system reserved. May not be modified.") });
        }

        var resourceClaims = getResourcesByClaimSetIdQuery.AllResources(claimSetId);
        var allResourcesIds = new List<int>();
        foreach (var resourceClaim in resourceClaims)
        {
            allResourcesIds.Add(resourceClaim.Id);
            if (resourceClaim.Children != null && resourceClaim.Children.Any())
            {
                foreach (var child in resourceClaim.Children)
                {
                    allResourcesIds.Add(child.Id);
                }
            }
        }
        if (!allResourcesIds.Contains(resourceClaimId))
        {
            throw new NotFoundException<int>("ResourceClaim", resourceClaimId);
        }
        else
        {
            overrideDefaultAuthorizationStrategyCommand.ResetAuthorizationStrategyOverrides(
                new OverrideAuthStrategyOnClaimSetModel()
                {
                    ClaimSetId = claimSetId,
                    ResourceClaimId = resourceClaimId
                });
        }

        return await Task.FromResult(Results.Ok());
    }


    public class OverrideAuthStategyOnClaimSetValidator : AbstractValidator<OverrideAuthStategyOnClaimSetRequest>
    {
        public OverrideAuthStategyOnClaimSetValidator(IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery, IGetAllAuthorizationStrategiesQuery getAllAuthorizationStrategiesQuery, IGetAllActionsQuery getAllActionsQuery, IGetClaimSetByIdQuery getClaimSetByIdQuery)
        {
            RuleFor(m => m.ClaimSetId).NotEqual(0);
            RuleFor(m => m.ResourceClaimId).NotEqual(0);
            RuleFor(m => m.ActionName).NotEmpty();
            RuleFor(m => m.AuthorizationStrategies).NotNull().NotEmpty();

            RuleFor(m => m).Custom((overrideAuthStategyOnClaimSetRequest, context) =>
            {

                var resourceClaim = getResourcesByClaimSetIdQuery.SingleResource(overrideAuthStategyOnClaimSetRequest.ClaimSetId, overrideAuthStategyOnClaimSetRequest.ResourceClaimId);
                if (resourceClaim == null)
                {
                    context.AddFailure("ResourceClaim", "Resource claim doesn't exist for the Claim set provided");
                }

                var claimSet = getClaimSetByIdQuery.Execute(overrideAuthStategyOnClaimSetRequest.ClaimSetId);
                if (!claimSet.IsEditable)
                {
                    context.AddFailure("ClaimSetId", $"Claim set ({claimSet.Name}) is system reserved. May not be modified.");
                }

                var authStrategies = getAllAuthorizationStrategiesQuery.Execute();
                foreach (var authStrategyName in overrideAuthStategyOnClaimSetRequest.AuthorizationStrategies!)
                {
                    var validAuthStrategyName = authStrategies
                      .FirstOrDefault(a => a.AuthStrategyName!.ToLower() == authStrategyName!.ToLower());

                    if (validAuthStrategyName == null)
                    {
                        context.AddFailure("AuthorizationStrategies", $"{authStrategyName} doesn't exist.");
                    }

                }

                var actionName = getAllActionsQuery.Execute().AsEnumerable()
                .FirstOrDefault(a => a.ActionName.ToLower() == overrideAuthStategyOnClaimSetRequest.ActionName!.ToLower());

                if (actionName == null)
                {
                    context.AddFailure("ActionName", "ActionName doesn't exist.");
                }
            });
        }
    }


    [SwaggerSchema(Title = "OverrideAuthStategyOnClaimSetRequest")]
    public class OverrideAuthStategyOnClaimSetRequest : OverrideAuthStrategyOnClaimSetModel
    {
        [SwaggerSchema(Description = "AuthorizationStrategy Names", Nullable = false)]
        public IEnumerable<string>? AuthorizationStrategies { get; set; }
    }
}
