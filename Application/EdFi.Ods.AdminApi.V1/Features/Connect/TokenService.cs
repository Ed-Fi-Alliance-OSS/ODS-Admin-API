// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenIddict.Abstractions;

namespace EdFi.Ods.AdminApi.V1.Features.Connect;

public interface ITokenService
{
    Task<ClaimsPrincipal> Handle(OpenIddictRequest request);
}

public class TokenService : ITokenService
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public TokenService(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public async Task<ClaimsPrincipal> Handle(OpenIddictRequest request)
    {
        if (!request.IsClientCredentialsGrantType())
        {
            throw new NotImplementedException("The specified grant type is not implemented");
        }

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new NotFoundException<string?>("Admin API Client", request.ClientId);

        if (!await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret!))
        {
            throw new AuthenticationException("Invalid Admin API Client key and secret");
        }

        var requestedScopes = request.GetScopes();
        var appScopes = (await _applicationManager.GetPermissionsAsync(application))
            .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
            .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .ToList();

        var missingScopes = requestedScopes.Where(s => !appScopes.Contains(s)).ToList();
        if (missingScopes.Any())
            throw new AuthenticationException($"Client is not allowed access to requested scope(s): {string.Join(", ", missingScopes)}");

        var displayName = await _applicationManager.GetDisplayNameAsync(application);

        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
        identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId!, OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(OpenIddictConstants.Claims.Name, displayName!, OpenIddictConstants.Destinations.AccessToken);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(requestedScopes);

        return principal;
    }
}
