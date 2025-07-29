// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.Features.ClaimSets.ResourceClaims;

public class DeleteResourceClaim : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}", Handle)
       .WithSummary("Deletes a resource claims association from a claimset")
       .WithRouteOptions(b => b.WithResponseCode(200))
       .BuildForVersions(AdminApiVersions.V2);
    }

    internal static async Task<IResult> Handle(IGetResourcesByClaimSetIdQuery getResourcesByClaimSetIdQuery,
        IGetClaimSetByIdQuery getClaimSetByIdQuery,
        IAuthStrategyResolver strategyResolver,
        IDeleteResouceClaimOnClaimSetCommand deleteResouceClaimOnClaimSetCommand,
        IMapper mapper, int claimSetId, int resourceClaimId)
    {
        var claimSet = getClaimSetByIdQuery.Execute(claimSetId);

        if (!claimSet.IsEditable)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(claimSetId), $"Claim set ({claimSet.Name}) is system reserved. May not be modified.") });
        }

        var resourceClaim = getResourcesByClaimSetIdQuery.SingleResource(claimSet.Id, resourceClaimId);
        if (resourceClaim == null)
        {
            throw new NotFoundException<int>("ResourceClaim", resourceClaimId);
        }
        else
        {
            deleteResouceClaimOnClaimSetCommand.Execute(claimSet.Id, resourceClaimId);
        }

        return await Task.FromResult(Results.Ok());
    }

}
