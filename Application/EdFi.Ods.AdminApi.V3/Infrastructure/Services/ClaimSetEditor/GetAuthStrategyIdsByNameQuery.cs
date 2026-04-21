// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public interface IGetAuthStrategyIdsByNameQuery
{
    List<int> Execute(IEnumerable<string> authStrategyNames);
}

public class GetAuthStrategyIdsByNameQuery : IGetAuthStrategyIdsByNameQuery
{
    private readonly IGetAllAuthorizationStrategiesQuery _getAllAuthorizationStrategiesQuery;

    public GetAuthStrategyIdsByNameQuery(IGetAllAuthorizationStrategiesQuery getAllAuthorizationStrategiesQuery)
    {
        _getAllAuthorizationStrategiesQuery = getAllAuthorizationStrategiesQuery;
    }

    public List<int> Execute(IEnumerable<string> authStrategyNames)
    {
        var allStrategies = _getAllAuthorizationStrategiesQuery.Execute();
        var ids = new List<int>();
        var unavailableAuthStrategies = new List<string>();

        foreach (var authStrategyName in authStrategyNames)
        {
            var authStrategy = allStrategies.FirstOrDefault(a =>
                authStrategyName.Equals(a.AuthStrategyName, StringComparison.InvariantCultureIgnoreCase));

            if (authStrategy is null)
            {
                unavailableAuthStrategies.Add(authStrategyName);
                continue;
            }

            ids.Add(authStrategy.AuthStrategyId);
        }

        if (unavailableAuthStrategies.Count > 0)
        {
            throw new AdminApiException($"Error transforming the ID for the AuthStrategyNames {string.Join(",", unavailableAuthStrategies)}");
        }

        return ids;
    }
}



