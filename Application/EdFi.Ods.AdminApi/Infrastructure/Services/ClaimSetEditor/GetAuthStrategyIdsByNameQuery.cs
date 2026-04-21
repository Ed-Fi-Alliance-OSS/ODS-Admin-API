// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public interface IGetAuthStrategyIdsByNameQuery
{
    List<int> Execute(IEnumerable<string> authStrategyNames);
}

public class GetAuthStrategyIdsByNameQuery(IGetAllAuthorizationStrategiesQueryCommon getAllAuthorizationStrategiesQuery)
    : GetAuthStrategyIdsByNameQueryBase(getAllAuthorizationStrategiesQuery), IGetAuthStrategyIdsByNameQuery
{
    public List<int> Execute(IEnumerable<string> authStrategyNames)
    {
        return ExecuteCore(authStrategyNames);
    }
}
