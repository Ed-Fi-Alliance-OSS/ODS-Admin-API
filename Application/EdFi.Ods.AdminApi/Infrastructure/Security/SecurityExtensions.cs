// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using EdFi.Ods.AdminApi.Features.Connect;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace EdFi.Ods.AdminApi.Infrastructure.Security;

public static class SecurityExtensions
{
    public static void AddSecurityUsingOpenIddict(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment
    )
    {
        bool useSelfContainedAuthorization = configuration.GetValue<bool>("AppSettings:UseSelfcontainedAuthorization");
        var issuer = useSelfContainedAuthorization
            ? configuration.Get<string>("Authentication:IssuerUrl")
            : configuration.Get<string>("Authentication:OIDC:Authority");
        var isDockerEnvironment = configuration.Get<bool>("EnableDockerEnvironment");

        //OpenIddict Server
        var signingKeyValue = configuration.Get<string>("Authentication:SigningKey");
        var signingKey = string.IsNullOrEmpty(signingKeyValue)
            ? null
            : new SymmetricSecurityKey(Convert.FromBase64String(signingKeyValue));
        var validateIssuerSigningKey = configuration.Get<bool>("Authentication:ValidateIssuerSigningKey");
        services
            .AddOpenIddict()
            .AddCore(opt =>
            {
                opt.UseEntityFrameworkCore()
                    .UseDbContext<AdminApiDbContext>()
                    .ReplaceDefaultEntities<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
            })
            .AddServer(opt =>
            {
                opt.AllowClientCredentialsFlow();
                opt.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
                opt.SetTokenEndpointUris(SecurityConstants.TokenEndpoint);

                opt.AddEphemeralEncryptionKey();
                opt.AddEphemeralSigningKey();
                opt.DisableAccessTokenEncryption();
                opt.SetIssuer(new Uri(issuer));

                if (!webHostEnvironment.IsDevelopment()) //Keys below will override Ephemeral / Dev Keys
                {
                    if (signingKey == null)
                    {
                        throw new AdminApiException("Invalid Configuration: Authentication:SigningKey is required.");
                    }
                    opt.AddSigningKey(signingKey);
                }
                foreach (var scope in SecurityConstants.Scopes.AllScopes)
                {
                    opt.RegisterScopes(scope.Scope);
                }
                var aspNetCoreBuilder = opt.UseAspNetCore().EnableTokenEndpointPassthrough();
                if (isDockerEnvironment)
                {
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
                }

                opt.AddEventHandler<ApplyTokenResponseContext>(builder =>
                    builder
                        .UseSingletonHandler<DefaultTokenResponseHandler>()
                        .SetType(OpenIddictServerHandlerType.Custom)
                );
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.Configure(options => options.TokenValidationParameters.IssuerSigningKey = signingKey);
            });

        //Application Security
        var authenticationBuilder = services.
            AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
        if (useSelfContainedAuthorization)
        {
            authenticationBuilder.AddJwtBearer(opt =>
            {
                opt.Authority = issuer;
                opt.SaveToken = true;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = validateIssuerSigningKey,
                    ValidIssuer = issuer,
                    RoleClaimType = "realm_access.roles",
                    IssuerSigningKey = signingKey
                };
                opt.RequireHttpsMetadata = !isDockerEnvironment;
            })
            .AddJwtBearer("IdentityProvider", options =>
            {
                var oidcIssuer = configuration.Get<string>("Authentication:OIDC:Authority");
                if (!String.IsNullOrEmpty(oidcIssuer))
                {
                    var oidcValidationCallback = configuration.Get<bool>("Authentication:OIDC:EnableServerCertificateCustomValidationCallback");
                    var requireHttpsMetadata = configuration.Get<bool>("Authentication:OIDC:RequireHttpsMetadata");
                    options.Authority = oidcIssuer;
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = requireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = validateIssuerSigningKey,
                        ValidIssuer = oidcIssuer,
                        RoleClaimType = "realm_access.roles",
                        IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                        {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                            var handler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => oidcValidationCallback
                            };
#pragma warning restore S4830
                            // Server certificates should be verified during SSL/TLS connections
                            // Get public keys from keycloak
                            var client = new HttpClient(handler);
                            var response = client.GetStringAsync(oidcIssuer + "/protocol/openid-connect/certs").Result;
                            var keys = JsonWebKeySet.Create(response).GetSigningKeys();
                            return keys;
                        }
                    };
                }
            });
        }
        else
        {
            authenticationBuilder.AddJwtBearer(options =>
            {
                var oidcIssuer = configuration.Get<string>("Authentication:OIDC:Authority");
                if (!String.IsNullOrEmpty(oidcIssuer))
                {
                    var oidcValidationCallback = configuration.Get<bool>("Authentication:OIDC:EnableServerCertificateCustomValidationCallback");
                    var requireHttpsMetadata = configuration.Get<bool>("Authentication:OIDC:RequireHttpsMetadata");
                    options.Authority = oidcIssuer;
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = requireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = configuration.Get<bool>("Authentication:OIDC:ValidateIssuer"),
                        ValidateIssuerSigningKey = validateIssuerSigningKey,
                        ValidIssuer = oidcIssuer,
                        RoleClaimType = "realm_access.roles",
                        IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                        {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                            var handler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => oidcValidationCallback
                            };
#pragma warning restore S4830
                            // Server certificates should be verified during SSL/TLS connections
                            // Get public keys from keycloak
                            var client = new HttpClient(handler);
                            var response = client.GetStringAsync(oidcIssuer + "/protocol/openid-connect/certs").Result;
                            var keys = JsonWebKeySet.Create(response).GetSigningKeys();
                            return keys;
                        }
                    };
                }
            });
        }

        services.AddAuthorization(opt =>
        {
            opt.DefaultPolicy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes()
                .RequireAssertion(context =>
                        !context.HasSucceeded
                        && context.User.HasClaim(c
                            => c.Type == OpenIddictConstants.Claims.Scope
                            && c.Value.Split(' ')
                            .ToList()
                            .Exists(scopeValue
                                => string.Equals(scopeValue, AuthorizationPolicies.DefaultScopePolicy.Scope, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                    )
                .Build();
            foreach (var policy in AuthorizationPolicies.RolePolicies)
            {
                opt.AddPolicy(policy.PolicyName, policyBuilder =>
                    policyBuilder.Requirements.Add(policy.RolesAuthorizationRequirement));
            }
            foreach (var scope in AuthorizationPolicies.ScopePolicies)
            {
                opt.AddPolicy(scope.PolicyName, policy =>
                {
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c
                        => c.Type == OpenIddictConstants.Claims.Scope
                        && c.Value.Split(' ')
                            .ToList()
                            .Exists(scopeValue
                                => string.Equals(scopeValue, scope.Scope, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(scopeValue, AuthorizationPolicies.DefaultScopePolicy.Scope, StringComparison.OrdinalIgnoreCase)
                            )
                    ));
                });
            }


        });
        services.AddSingleton<IAuthorizationHandler, RolesAuthorizationHandler>();
        // Controllers to hide from Swagger conditionally
        var controllerNamesToHide = new List<string> { "ConnectController" };
        //Security Endpoints
        services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<IRegisterService, RegisterService>();
        services.AddControllers(options =>
        {
            options.Conventions.Add(new SwaggerHideControllerConvention(configuration, controllerNamesToHide));
        });
    }

    public class DefaultTokenResponseHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
    {
        private const string DENIED_AUTHENTICATION_MESSAGE =
            "Access Denied. Please review your information and try again.";

        public ValueTask HandleAsync(ApplyTokenResponseContext context)
        {
            var response = context.Response;

            if (
                string.Equals(
                    response.Error,
                    OpenIddictConstants.Errors.InvalidGrant,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    response.Error,
                    OpenIddictConstants.Errors.UnsupportedGrantType,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    response.Error,
                    OpenIddictConstants.Errors.InvalidClient,
                    StringComparison.Ordinal
                )
                || string.Equals(
                    response.Error,
                    OpenIddictConstants.Errors.InvalidScope,
                    StringComparison.Ordinal
                )
            )
            {
                response.Error = OpenIddictConstants.Errors.InvalidClient;
                response.ErrorDescription = DENIED_AUTHENTICATION_MESSAGE;
                response.ErrorUri = "";
            }

            return default;
        }
    }
}
