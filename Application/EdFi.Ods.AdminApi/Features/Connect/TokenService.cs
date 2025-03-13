// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Security.Authentication;
using System.Security.Claims;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenIddict.Abstractions;

namespace EdFi.Ods.AdminApi.Features.Connect;

public interface ITokenService
{
    Task<ClaimsPrincipal> Handle(OpenIddictRequest request);
}

public class TokenService(IOpenIddictApplicationManager applicationManager, IConfiguration configuration) : ITokenService
{
    private readonly IOpenIddictApplicationManager _applicationManager = applicationManager;
    private readonly IConfiguration _configuration = configuration;
    private const string DENIED_AUTHENTICATION_MESSAGE = "Access Denied. Please review your information and try again.";

    public async Task<ClaimsPrincipal> Handle(OpenIddictRequest request)
    {
        if (!request.IsClientCredentialsGrantType())
        {
            throw new NotImplementedException(DENIED_AUTHENTICATION_MESSAGE);
        }

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!) ??
            throw new NotFoundException<string?>("Access Denied", DENIED_AUTHENTICATION_MESSAGE);

        if (!await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret!))
        {
            throw new AuthenticationException(DENIED_AUTHENTICATION_MESSAGE);
        }

        var requestedScopes = request.GetScopes();
        var appScopes = (await _applicationManager.GetPermissionsAsync(application))
            .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
            .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .ToList();

        var missingScopes = requestedScopes.Where(s => !appScopes.Contains(s)).ToList();
        if (missingScopes.Count != 0)
            throw new AuthenticationException(DENIED_AUTHENTICATION_MESSAGE);

        var displayName = await _applicationManager.GetDisplayNameAsync(application);
        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
        identity.AddClaim(OpenIddictConstants.Claims.Subject, request.ClientId!, OpenIddictConstants.Destinations.AccessToken);
        identity.AddClaim(OpenIddictConstants.Claims.Name, displayName!, OpenIddictConstants.Destinations.AccessToken);
        var roles = Roles.AllRoles.Select(obj => obj.RoleName).ToList();
        var rolesClaim = _configuration?.GetValue<string>("AppSettings:roleClaimAttribute") ?? "roles";
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(rolesClaim, role, OpenIddictConstants.Destinations.AccessToken));
        }
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(requestedScopes);
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
        }
        return principal;
    }
}
