// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Features.Actions;

public class ReadActions : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/actions", GetActions)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponse<ActionModel[]>(200))
           .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetActions(IGetAllActionsQuery getAllActionsQuery, IMapper mapper, int? offset, int? limit, string? orderBy, string? direction, int? id, string? name)
    {
        var actions = mapper.Map<List<ActionModel>>(
            getAllActionsQuery.Execute(
                new CommonQueryParams(offset, limit, orderBy, direction),
                id,
                name));
        return Task.FromResult(Results.Ok(actions));
    }
}
