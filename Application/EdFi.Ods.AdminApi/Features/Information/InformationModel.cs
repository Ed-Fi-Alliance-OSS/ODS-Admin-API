// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace EdFi.Ods.AdminApi.Features.Information;

[SwaggerSchema(Title = "Information")]
public class InformationResult
{
    public InformationResult(
        string version,
        string build,
        string specificationVersion,
        string? applicationName = null,
        string? informationalVersion = null,
        ApiUrlsResult? urls = null)
    {
        Build = build;
        Version = version;
        SpecificationVersion = specificationVersion;
        ApplicationName = applicationName;
        InformationalVersion = informationalVersion;
        Urls = urls;
    }

    [SwaggerSchema("Application version", Nullable = false)]
    public string Version { get; }
    [SwaggerSchema("Build / release version", Nullable = false)]
    public string Build { get; }
    [SwaggerSchema("Management API specification version", Nullable = false)]
    public string SpecificationVersion { get; }
    [SwaggerSchema("Application name", Nullable = true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ApplicationName { get; }
    [SwaggerSchema("Informational/semantic version", Nullable = true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InformationalVersion { get; }
    [SwaggerSchema("Related URLs", Nullable = true)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiUrlsResult? Urls { get; }
}

[SwaggerSchema(Title = "ApiUrls")]
public class ApiUrlsResult
{
    public ApiUrlsResult(string openApiMetadata)
    {
        OpenApiMetadata = openApiMetadata;
    }

    [SwaggerSchema("Absolute URL to the OpenAPI/metadata document", Nullable = false)]
    public string OpenApiMetadata { get; }
}
