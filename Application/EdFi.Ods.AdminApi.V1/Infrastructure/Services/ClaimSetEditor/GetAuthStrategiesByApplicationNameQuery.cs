// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class GetAuthStrategiesByApplicationNameQuery : IGetAuthStrategiesByApplicationNameQuery
{

    private readonly ISecurityContext _securityContext;

    public GetAuthStrategiesByApplicationNameQuery(ISecurityContext securityContext)
    {
        _securityContext = securityContext;
    }

    public List<AuthorizationStrategy> Execute(string applicationName)
    {
        var authStrategies = GetAuthStrategiesByApplicationName(applicationName);
        return authStrategies.ToList();
    }

    private AuthorizationStrategy[] GetAuthStrategiesByApplicationName(string applicationName)
    {
        return _securityContext.AuthorizationStrategies
            .Where(x => x.Application.ApplicationName == applicationName)
            .OrderBy(x => x.AuthorizationStrategyName)
            .Select(x => new AuthorizationStrategy
            {
                AuthStrategyId = x.AuthorizationStrategyId,
                AuthStrategyName = x.DisplayName
            }).ToArray();
    }
}

public interface IGetAuthStrategiesByApplicationNameQuery
{
    List<AuthorizationStrategy> Execute(string securityContextApplicationName);
}
