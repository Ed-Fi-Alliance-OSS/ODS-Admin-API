// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using CommonAuthorizationStrategy = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
public interface IGetAllAuthorizationStrategiesQuery
{
    IReadOnlyList<AuthorizationStrategy> Execute();
}

public class GetAllAuthorizationStrategiesQuery(ISecurityContext securityContext)
    : GetAllAuthorizationStrategiesQueryBase(securityContext), IGetAllAuthorizationStrategiesQuery, IGetAllAuthorizationStrategiesQueryCommon
{
    IReadOnlyList<CommonAuthorizationStrategy> IGetAllAuthorizationStrategiesQueryCommon.Execute()
    {
        return base.Execute();
    }

    public new IReadOnlyList<AuthorizationStrategy> Execute()
    {
        return base.Execute()
            .Select(Map)
            .ToList();
    }

    private static AuthorizationStrategy Map(CommonAuthorizationStrategy strategy)
    {
        return new AuthorizationStrategy
        {
            AuthStrategyId = strategy.AuthStrategyId,
            AuthStrategyName = strategy.AuthStrategyName,
            IsInheritedFromParent = strategy.IsInheritedFromParent
        };
    }
}

