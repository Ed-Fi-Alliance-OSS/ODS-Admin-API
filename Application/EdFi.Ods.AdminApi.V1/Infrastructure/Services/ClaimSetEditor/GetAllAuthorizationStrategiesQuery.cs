// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
public interface IGetAllAuthorizationStrategiesQuery
{
    IReadOnlyList<AuthorizationStrategy> Execute();
}

public class GetAllAuthorizationStrategiesQuery : IGetAllAuthorizationStrategiesQuery
{
    private readonly ISecurityContext _securityContext;

    public GetAllAuthorizationStrategiesQuery(ISecurityContext securityContext)
    {
        _securityContext = securityContext;
    }

    public IReadOnlyList<AuthorizationStrategy> Execute()
    {
        return _securityContext.AuthorizationStrategies
            .OrderBy(x => x.AuthorizationStrategyName)
            .Select(x => new AuthorizationStrategy
            {
                AuthStrategyId = x.AuthorizationStrategyId,
                AuthStrategyName = x.AuthorizationStrategyName,
                DisplayName = x.DisplayName
            }).ToList();
    }
}
