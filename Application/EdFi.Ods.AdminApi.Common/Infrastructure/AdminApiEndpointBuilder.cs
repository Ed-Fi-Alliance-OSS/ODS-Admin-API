// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

public class AdminApiEndpointBuilder
{
    private AdminApiEndpointBuilder(IEndpointRouteBuilder endpoints,
        HttpVerb verb, string route, Delegate handler)
    {
        _endpoints = endpoints;
        _verb = verb;
        _route = route.Trim('/');
        _handler = handler;
        _pluralResourceName = _route.Split('/')[0];
    }

    private readonly IEndpointRouteBuilder _endpoints;
    private readonly HttpVerb? _verb;
    private readonly string _route;
    private readonly Delegate? _handler;
    private readonly List<Action<RouteHandlerBuilder>> _routeOptions = [];
    private readonly string _pluralResourceName;
    private bool _allowAnonymous = false;
    private IEnumerable<string> _authorizationPolicies = [];

    public static AdminApiEndpointBuilder MapGet(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.GET, route, handler);

    public static AdminApiEndpointBuilder MapPost(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.POST, route, handler);

    public static AdminApiEndpointBuilder MapPut(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.PUT, route, handler);

    public static AdminApiEndpointBuilder MapDelete(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.DELETE, route, handler);

    /// <summary>
    /// Includes the specified authorization policy in the endpoint.
    /// </summary>
    /// <param name="authorizationPolicies">List of authorization Policies to validate</param>
    /// <returns></returns>
    public AdminApiEndpointBuilder RequireAuthorization(IEnumerable<PolicyDefinition> authorizationPolicies)
    {
        _authorizationPolicies = authorizationPolicies.Select(policy => policy.PolicyName).ToList();
        return this;
    }

    /// <summary>
    /// Includes the specified authorization policy in the endpoint.
    /// </summary>
    /// <param name="authorizationPolicies">List of authorization Policies to validate</param>
    /// <returns></returns>
    public AdminApiEndpointBuilder RequireAuthorization(PolicyDefinition authorizationPolicies)
    {
        _authorizationPolicies = [authorizationPolicies.PolicyName];
        return this;
    }

    public void BuildForVersions(params AdminApiVersions.AdminApiVersion[] versions)
    {
        BuildForVersions(string.Empty, false, versions);
    }

    public void BuildForVersions(string authorizationPolicy, bool display409 = false, params AdminApiVersions.AdminApiVersion[] versions)
    {
        if (versions.Length == 0)
            throw new ArgumentException("Must register for at least 1 version");
        if (_route == null)
            throw new InvalidOperationException("Invalid endpoint registration. Route must be specified");
        if (_handler == null)
            throw new InvalidOperationException("Invalid endpoint registration. Handler must be specified");

        foreach (var version in versions)
        {
            if (version == null)
                throw new ArgumentException("Version cannot be null");

            var versionedRoute = $"/{version}/{_route}";

            var builder = _verb switch
            {
                HttpVerb.GET => _endpoints.MapGet(versionedRoute, _handler),
                HttpVerb.POST => _endpoints.MapPost(versionedRoute, _handler),
                HttpVerb.PUT => _endpoints.MapPut(versionedRoute, _handler),
                HttpVerb.DELETE => _endpoints.MapDelete(versionedRoute, _handler),
                _ => throw new ArgumentOutOfRangeException($"Unconfigured HTTP verb for mapping: {_verb}")
            };

            if (_allowAnonymous)
            {
                builder.AllowAnonymous();
            }
            else
            {
                if (_authorizationPolicies.Any())
                {
                    builder.RequireAuthorization(_authorizationPolicies.ToArray());
                }
                else if (!string.IsNullOrWhiteSpace(authorizationPolicy))
                {
                    builder.RequireAuthorization(authorizationPolicy);
                }
                else
                {
                    builder.RequireAuthorization();
                }
            }

            builder.WithGroupName(version.ToString());
            builder.WithResponseCode(401, "Unauthorized. The request requires authentication");
            builder.WithResponseCode(403, "Forbidden. The request is authenticated, but not authorized to access this resource");
            if (display409)
                builder.WithResponseCode(409, "Conflict. The request is authenticated, but it has a conflict with an existing element");
            builder.WithResponseCode(500, FeatureCommonConstants.InternalServerErrorResponseDescription);

            if (_route.Contains("id", StringComparison.InvariantCultureIgnoreCase))
            {
                builder.WithResponseCode(404, "Not found. A resource with given identifier could not be found.");
            }

            if (_verb is HttpVerb.PUT or HttpVerb.POST)
            {
                builder.WithResponseCode(400, FeatureCommonConstants.BadRequestResponseDescription);
            }

            foreach (var action in _routeOptions)
            {
                action(builder);
            }
        }
    }

    public AdminApiEndpointBuilder WithRouteOptions(Action<RouteHandlerBuilder> routeHandlerBuilderAction)
    {
        _routeOptions.Add(routeHandlerBuilderAction);
        return this;
    }

    public AdminApiEndpointBuilder WithDefaultSummaryAndDescription()
    {
        var summary = _verb switch
        {
            HttpVerb.GET => _route.Contains("id") ? $"Retrieves a specific {_pluralResourceName.ToSingleEntity()} based on the identifier." : $"Retrieves all {_pluralResourceName}.",
            HttpVerb.POST => $"Creates {_pluralResourceName.ToSingleEntity()} based on the supplied values.",
            HttpVerb.PUT => $"Updates {_pluralResourceName.ToSingleEntity()} based on the resource identifier.",
            HttpVerb.DELETE => $"Deletes an existing {_pluralResourceName.ToSingleEntity()} using the resource identifier.",
            _ => throw new ArgumentOutOfRangeException($"Unconfigured HTTP verb for default description {_verb}")
        };

        var description = _verb switch
        {
            HttpVerb.GET => "This GET operation provides access to resources using the \"Get\" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).",
            HttpVerb.POST => "The POST operation can be used to create or update resources. In database terms, this is often referred to as an \"upsert\" operation (insert + update). Clients should NOT include the resource \"id\" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.",
            HttpVerb.PUT => "The PUT operation is used to update a resource by identifier. If the resource identifier (\"id\") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.",
            HttpVerb.DELETE => "The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).",
            _ => throw new ArgumentOutOfRangeException($"Unconfigured HTTP verb for default description {_verb}")
        };

        return WithSummaryAndDescription(summary, description);
    }

    public AdminApiEndpointBuilder WithSummary(string summary)
    {
        _routeOptions.Add(rhb => rhb.WithMetadata(new SwaggerOperationAttribute(summary)));
        return this;
    }

    public AdminApiEndpointBuilder WithSummaryAndDescription(string summary, string description)
    {
        _routeOptions.Add(rhb => rhb.WithMetadata(new SwaggerOperationAttribute(summary, description)));
        return this;
    }

    public AdminApiEndpointBuilder AllowAnonymous()
    {
        _allowAnonymous = true;
        return this;
    }

    private enum HttpVerb { GET, POST, PUT, DELETE }
}
