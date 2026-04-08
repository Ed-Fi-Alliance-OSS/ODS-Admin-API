// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using FluentValidation.Results;

namespace EdFi.Ods.AdminApi.Features.DbInstances;

public class DeleteDbInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapDelete(endpoints, "/dbinstances/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static Task<IResult> Handle(
        IGetDbInstanceByIdQuery getDbInstanceByIdQuery,
        IDeleteDbInstanceCommand deleteDbInstanceCommand,
        int id
    )
    {
        var dbInstance = getDbInstanceByIdQuery.Execute(id);
        if (dbInstance is null)
            throw new NotFoundException<int>("dbInstance", id);

        if (dbInstance.Status == DbInstanceStatus.Deleted.ToString())
            throw new NotFoundException<int>("dbInstance", id);

        var blockingMessage = GetBlockingStatusMessage(dbInstance.Status);
        if (blockingMessage is not null)
            throw new ValidationException([new ValidationFailure(nameof(id), blockingMessage)]);

        deleteDbInstanceCommand.Execute(id);
        return Task.FromResult(Results.NoContent());
    }

    private static string? GetBlockingStatusMessage(string status)
    {
        if (Enum.TryParse<DbInstanceStatus>(status, out var parsed))
        {
            return parsed switch
            {
                DbInstanceStatus.Pending => "DbInstance is being provisioned. Wait for the creation job to complete before deleting.",
                DbInstanceStatus.InProgress => "DbInstance is currently being provisioned. Wait for the creation job to complete before deleting.",
                DbInstanceStatus.PendingDelete => "DbInstance is already queued for deletion.",
                DbInstanceStatus.Error => "DbInstance encountered an error during provisioning. Check job status before retrying.",
                _ => null,
            };
        }

        return null;
    }
}
