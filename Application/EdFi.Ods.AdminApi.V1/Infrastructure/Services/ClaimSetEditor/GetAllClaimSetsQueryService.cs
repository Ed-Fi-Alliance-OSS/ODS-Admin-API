// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;


public class GetAllClaimSetsQueryService(ISecurityContext securityContext, IOptions<AppSettings> options)
{
    private readonly ISecurityContext _securityContext = securityContext;
    private readonly IOptions<AppSettings> _options = options;

    public IReadOnlyList<ClaimSet> Execute()
    {
        return [.. _securityContext.ClaimSets
            .Select(x => new ClaimSet
            {
                Id = x.ClaimSetId,
                Name = x.ClaimSetName,
                IsEditable = !x.ForApplicationUseOnly && !x.IsEdfiPreset &&
                !Constants.SystemReservedClaimSets.Contains(x.ClaimSetName)
            })
            .Distinct()
            .OrderBy(x => x.Name)];
    }

    public IReadOnlyList<ClaimSet> Execute(CommonQueryParams commonQueryParams)
    {
        return [.. _securityContext.ClaimSets
            .Select(x => new ClaimSet
            {
                Id = x.ClaimSetId,
                Name = x.ClaimSetName,
                IsEditable = !x.ForApplicationUseOnly && !x.IsEdfiPreset &&
                !Constants.SystemReservedClaimSets.Contains(x.ClaimSetName)
            })
            .Distinct()
            .OrderBy(x => x.Name)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)];
    }
}
