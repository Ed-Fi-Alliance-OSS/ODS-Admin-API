// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;
using ClaimSet = EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.ClaimSet;
using CommonClaimSet = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetAllClaimSetsQuery
{
    IReadOnlyList<ClaimSet> Execute();
    IReadOnlyList<ClaimSet> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetAllClaimSetsQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    : GetAllClaimSetsQueryBase(securityContext, options), IGetAllClaimSetsQuery
{
    public IReadOnlyList<ClaimSet> Execute()
    {
        return ExecuteCore()
            .Select(Map)
            .ToList();
    }

    public IReadOnlyList<ClaimSet> Execute(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        return ExecuteCore(commonQueryParams, id, name)
            .Select(Map)
            .ToList();
    }

    private static ClaimSet Map(CommonClaimSet claimSet)
    {
        return new ClaimSet
        {
            Id = claimSet.Id,
            Name = claimSet.Name,
            IsEditable = claimSet.IsEditable,
            ApplicationsCount = claimSet.ApplicationsCount
        };
    }
}



