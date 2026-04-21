// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetResourceClaimActionAuthorizationStrategiesQuery
{
    public IReadOnlyList<ResourceClaimActionAuthStrategyModel> Execute(CommonQueryParams commonQueryParams, string? resourceName);
}

public class GetResourceClaimActionAuthorizationStrategiesQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    : GetResourceClaimActionAuthorizationStrategiesQueryBase(securityContext, options), IGetResourceClaimActionAuthorizationStrategiesQuery
{
    public IReadOnlyList<ResourceClaimActionAuthStrategyModel> Execute(CommonQueryParams commonQueryParams, string? resourceName)
    {
        return ExecuteCore(commonQueryParams, resourceName);
    }
}
