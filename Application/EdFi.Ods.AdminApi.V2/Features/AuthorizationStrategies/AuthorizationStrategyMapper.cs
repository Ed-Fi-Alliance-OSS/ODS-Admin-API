// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V2.Features.AuthorizationStrategies;

public static class AuthorizationStrategyMapper
{
    public static AuthorizationStrategyModel ToModel(AuthorizationStrategy source)
    {
        return new AuthorizationStrategyModel
        {
            AuthStrategyId = source.AuthorizationStrategyId,
            AuthStrategyName = source.AuthorizationStrategyName,
            DisplayName = source.DisplayName
        };
    }

    public static List<AuthorizationStrategyModel> ToModelList(IEnumerable<AuthorizationStrategy> source)
    {
        return source.Select(ToModel).ToList();
    }
}
