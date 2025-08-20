// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Features;
using EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public class AdminApiEndpointBuilder
{
    private AdminApiEndpointBuilder(IEndpointRouteBuilder endpoints,
        HttpVerb verb, string route, Delegate handler)
    {
        _endpoints = endpoints;
        _verb = verb;
        _route = route.Trim('/');
        _handler = handler;
        _pluralResourceName = _route.Split('/').First();
    }

    private readonly IEndpointRouteBuilder _endpoints;
    private readonly HttpVerb? _verb;
    private readonly string _route;
    private readonly Delegate? _handler;
    private readonly List<Action<RouteHandlerBuilder>> _routeOptions = new();
    private readonly string _pluralResourceName;
    private bool _allowAnonymous = false;

    public static AdminApiEndpointBuilder MapGet(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.GET, route, handler);

    public static AdminApiEndpointBuilder MapPost(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.POST, route, handler);

    public static AdminApiEndpointBuilder MapPut(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.PUT, route, handler);

    public static AdminApiEndpointBuilder MapDelete(IEndpointRouteBuilder endpoints, string route, Delegate handler)
        => new(endpoints, HttpVerb.DELETE, route, handler);

    public void BuildForVersions(params AdminApiVersions.AdminApiVersion[] versions)
    {
        if (versions.Length == 0) throw new ArgumentException("Must register for at least 1 version");
        if (_route == null) throw new Exception("Invalid endpoint registration. Route must be specified");
        if (_handler == null) throw new Exception("Invalid endpoint registration. Handler must be specified");

        foreach (var version in versions)
        {
            if (version == null) throw new ArgumentException("Version cannot be null");

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
                builder.RequireAuthorization();
            }

            builder.WithGroupName(version.ToString());
            builder.WithResponseCode(401, "Unauthorized. The request requires authentication");
            builder.WithResponseCode(403, "Forbidden. The request is authenticated, but not authorized to access this resource");
            builder.WithResponseCode(500, FeatureConstants.InternalServerErrorResponseDescription);

            if (_route.Contains("id", StringComparison.InvariantCultureIgnoreCase))
            {
                builder.WithResponseCode(404, "Not found. A resource with given identifier could not be found.");
            }

            if (_verb is HttpVerb.PUT or HttpVerb.POST)
            {
                builder.WithResponseCode(400, FeatureConstants.BadRequestResponseDescription);
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

    public AdminApiEndpointBuilder WithDefaultDescription()
    {
        var description = _verb switch
        {
            HttpVerb.GET => _route.Contains("id") ? $"Retrieves a specific {_pluralResourceName.ToSingleEntity()} based on the identifier." : $"Retrieves all {_pluralResourceName}.",
            HttpVerb.POST => $"Creates {_pluralResourceName.ToSingleEntity()} based on the supplied values.",
            HttpVerb.PUT => $"Updates {_pluralResourceName.ToSingleEntity()} based on the resource identifier.",
            HttpVerb.DELETE => $"Deletes an existing {_pluralResourceName.ToSingleEntity()} using the resource identifier.",
            _ => throw new ArgumentOutOfRangeException($"Unconfigured HTTP verb for default description {_verb}")
        };

        return WithDescription(description);
    }

    public AdminApiEndpointBuilder WithDescription(string description)
    {
        _routeOptions.Add(rhb => rhb.WithMetadata(new SwaggerOperationAttribute(description)));
        return this;
    }

    public AdminApiEndpointBuilder AllowAnonymous()
    {
        _allowAnonymous = true;
        return this;
    }

    private enum HttpVerb { GET, POST, PUT, DELETE }
}
