// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;
using CommonResourceClaim = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ResourceClaim;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetResourceClaimsQuery
{
    IEnumerable<ResourceClaim> Execute();
    IEnumerable<ResourceClaim> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetResourceClaimsQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    : GetResourceClaimsQueryBase(securityContext, options), IGetResourceClaimsQuery
{
    public IEnumerable<ResourceClaim> Execute()
    {
        return ExecuteCore()
            .Select(Map)
            .ToList();
    }

    public IEnumerable<ResourceClaim> Execute(CommonQueryParams commonQueryParams, int? id, string? name)
    {
        return ExecuteCore(commonQueryParams, id, name)
            .Select(Map)
            .ToList();
    }

    private static ResourceClaim Map(CommonResourceClaim resourceClaim)
    {
        return new ResourceClaim
        {
            Id = resourceClaim.Id,
            Name = resourceClaim.Name,
            ParentId = resourceClaim.ParentId,
            ParentName = resourceClaim.ParentName,
            Children = resourceClaim.Children.Select(Map).ToList()
        };
    }
}
