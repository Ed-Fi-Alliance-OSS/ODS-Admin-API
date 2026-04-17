// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;

namespace EdFi.Ods.AdminApi.V3.Features.ClaimSets;

public class DeleteClaimSet : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/claimSets/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200, FeatureCommonConstants.DeletedSuccessResponseDescription))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public Task<IResult> Handle(IDeleteClaimSetCommand deleteClaimSetCommand, [FromServices] IGetClaimSetByIdQuery getClaimSetByIdQuery, IGetApplicationsByClaimSetIdQuery getApplications, int id)
    {
        CheckClaimSetExists(id, getClaimSetByIdQuery);
        CheckAgainstDeletingClaimSetsWithApplications(id, getApplications);

        try
        {
            deleteClaimSetCommand.Execute(new DeleteClaimSetModel { Id = id });
        }
        catch (AdminApiException exception)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(id), exception.Message) });
        }

        return Task.FromResult(Results.Ok("ClaimSet".ToJsonObjectResponseDeleted()));
    }

    private static void CheckClaimSetExists(int id, IGetClaimSetByIdQuery query)
    {
        var claimSet = query.Execute(id);
        if (claimSet != null && !claimSet.IsEditable)
        {
            throw new ValidationException(new[] { new ValidationFailure(nameof(id),
                $"Claim set ({claimSet.Name}) is system reserved. May not be modified.") });
        }
    }

    private static void CheckAgainstDeletingClaimSetsWithApplications(int id, IGetApplicationsByClaimSetIdQuery getApplications)
    {
        var associatedApplicationsCount = getApplications.Execute(id).Count();
        if (associatedApplicationsCount > 0)
            throw new ValidationException(new[] { new ValidationFailure(nameof(id),
                $"Cannot delete this claim set. This claim set has {associatedApplicationsCount} associated application(s).") });
    }
}
