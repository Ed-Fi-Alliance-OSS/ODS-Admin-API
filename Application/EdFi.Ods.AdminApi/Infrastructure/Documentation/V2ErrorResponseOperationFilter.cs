// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Documentation;

/// <summary>
/// Documents the error bodies returned by the V2 API. V2 does not use RFC 7807 problem details
/// (that is V3 only, see <see cref="ProblemDetailsResponseOperationFilter"/>), it returns the
/// legacy Message / NotFound / ValidationError / OAuthError bodies. 401 and 403 have no body.
/// </summary>
public class V2ErrorResponseOperationFilter : IOperationFilter
{
    public const string MessageResultId = "messageResult";
    public const string NotFoundResultId = "notFoundResult";
    public const string ValidationErrorResultId = "validationErrorResult";
    public const string OAuthErrorResultId = "oauthErrorResult";

    private const string ProblemJsonContentType = "application/problem+json";
    private const string JsonContentType = "application/json";
    private const string TokenRoute = "connect/token";
    private const string RegisterRoute = "connect/register";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses is null)
            return;

        var route = context.ApiDescription.RelativePath?.Trim('/') ?? string.Empty;

        foreach (var (statusCode, openApiResponse) in operation.Responses)
        {
            if (openApiResponse is not OpenApiResponse response || response.Content is { Count: > 0 })
                continue;

            var content = BuildContent(statusCode, route);

            if (content is null)
                continue;

            EnsureSchemas(context.SchemaRepository);
            response.Content = content;
        }
    }

    private static Dictionary<string, OpenApiMediaType>? BuildContent(string statusCode, string route) =>
        statusCode switch
        {
            "400" when route.Equals(TokenRoute, StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, OpenApiMediaType>
                {
                    [ProblemJsonContentType] = new() { Schema = Reference(OAuthErrorResultId) },
                    [JsonContentType] = new() { Schema = Reference(OAuthErrorResultId) }
                },
            "400" when route.Equals(RegisterRoute, StringComparison.OrdinalIgnoreCase) =>
                ProblemJson(Reference(ValidationErrorResultId)),
            "400" => ProblemJson(
                new OpenApiSchema
                {
                    OneOf = [Reference(ValidationErrorResultId), Reference(MessageResultId)]
                }),
            "404" => ProblemJson(Reference(NotFoundResultId)),
            "409" or "500" or "503" => ProblemJson(Reference(MessageResultId)),
            _ => null
        };

    private static Dictionary<string, OpenApiMediaType> ProblemJson(IOpenApiSchema schema) =>
        new() { [ProblemJsonContentType] = new OpenApiMediaType { Schema = schema } };

    private static OpenApiSchemaReference Reference(string id) => new(id);

    private static void EnsureSchemas(SchemaRepository repository)
    {
        AddIfMissing(
            repository,
            MessageResultId,
            () => new OpenApiSchema
            {
                Title = "Message",
                Type = JsonSchemaType.Object,
                Description =
                    "Error body returned for handled Admin API errors (such as conflicts or reserved resources), malformed JSON request bodies, a request to the wrong API version for this instance, and unhandled server errors.",
                Required = new HashSet<string> { "message" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["message"] = StringProperty("Human-readable explanation of the error")
                },
                AdditionalPropertiesAllowed = false
            });

        AddIfMissing(
            repository,
            NotFoundResultId,
            () => new OpenApiSchema
            {
                Title = "NotFound",
                Type = JsonSchemaType.Object,
                Description = "Error body returned when the requested resource could not be found.",
                Required = new HashSet<string> { "title" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["title"] = StringProperty(
                        "Human-readable explanation identifying the resource that was not found, for example \"Not found: vendor with ID 5. It may have been recently deleted.\"")
                },
                AdditionalPropertiesAllowed = false
            });

        AddIfMissing(
            repository,
            ValidationErrorResultId,
            () => new OpenApiSchema
            {
                Title = "ValidationError",
                Type = JsonSchemaType.Object,
                Description = "Error body returned when the request fails validation.",
                Required = new HashSet<string> { "title", "errors" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["title"] = StringProperty("Always \"Validation failed\"."),
                    ["errors"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Description = "Validation failures keyed by property name.",
                        AdditionalProperties = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Array,
                            Items = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                },
                AdditionalPropertiesAllowed = false
            });

        AddIfMissing(
            repository,
            OAuthErrorResultId,
            () => new OpenApiSchema
            {
                Title = "OAuthError",
                Type = JsonSchemaType.Object,
                Description =
                    "OAuth 2.0 error body (RFC 6749, section 5.2) returned by the token endpoint. An invalid_scope error is returned with a content type of application/problem+json and also includes title and status; other errors are returned as application/json.",
                Required = new HashSet<string> { "error" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["error"] = StringProperty(
                        "OAuth 2.0 error code, for example invalid_request, invalid_client, invalid_scope or unsupported_grant_type."),
                    ["error_description"] = StringProperty("Human-readable explanation of the error"),
                    ["error_uri"] = StringProperty("URI identifying a page with information about the error"),
                    ["title"] = StringProperty("Short summary of the error. Present on invalid_scope errors."),
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int32",
                        Description = "HTTP status code. Present on invalid_scope errors."
                    }
                },
                AdditionalPropertiesAllowed = false
            });
    }

    private static OpenApiSchema StringProperty(string description) =>
        new() { Type = JsonSchemaType.String, Description = description };

    private static void AddIfMissing(SchemaRepository repository, string id, Func<OpenApiSchema> create)
    {
        if (!repository.Schemas.ContainsKey(id))
            repository.AddDefinition(id, create());
    }
}
