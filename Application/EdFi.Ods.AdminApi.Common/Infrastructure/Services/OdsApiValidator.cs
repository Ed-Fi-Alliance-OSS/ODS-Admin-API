// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Services;

public interface IOdsApiValidator
{
    Task<OdsApiValidatorResult> Validate(string apiServerUrl);
}

public class OdsApiValidatorResult
{
    public bool IsValidOdsApi { get; set; }

    public Version? Version { get; set; }

    public Exception? Exception { get; set; }
}

public class OdsApiValidator : IOdsApiValidator
{
    private readonly ISimpleGetRequest _getRequest;

    public OdsApiValidator(ISimpleGetRequest getRequest) => _getRequest = getRequest;

    public async Task<OdsApiValidatorResult> Validate(string apiServerUrl)
    {
        try
        {
            var contentAsString = await _getRequest.DownloadString(apiServerUrl);

            var schemaJson = GetSchemaJson();

            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            var parsedContent = ParseJson(contentAsString);

            var errors = schema.Validate(parsedContent);

            if (errors.Count == 0 && parsedContent.TryGetValue("version", out var version))
            {
                return new OdsApiValidatorResult
                {
                    Version = Version.Parse(version.Value<string>()!),
                    IsValidOdsApi = true
                };
            }

            return errors.Count == 0
                ? new OdsApiValidatorResult { IsValidOdsApi = true }
                : InvalidOdsApiValidatorResult("The API provided does not have a valid root JSON document.");
        }
        catch (InvalidOperationException exception)
        {
            return InvalidOdsApiValidatorResult(exception.Message);
        }
        catch (JsonException exception)
        {
            return InvalidOdsApiValidatorResult($"The API provided does not have a valid root JSON document. JSON Parser Exception encountered: {exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            return InvalidOdsApiValidatorResult(exception.Message, exception.StatusCode ?? HttpStatusCode.ServiceUnavailable);
        }
        catch (Exception exception)
        {
            return InvalidOdsApiValidatorResult(exception.Message);
        }

        string GetSchemaJson() => @"{
                              ""type"": ""object"",
                              ""properties"": {
                                ""version"": {
                                  ""type"": ""string""
                                },
                                ""informationalVersion"": {
                                  ""type"": ""string""
                                },
                                ""suite"": {
                                  ""type"": ""string""
                                },
                                ""build"": {
                                  ""type"": ""string""
                                },
                                ""apiMode"": {
                                  ""type"": ""string""
                                },
                                ""dataModels"": {
                                  ""type"": ""array"",
                                  ""items"": [
                                    {
                                      ""type"": ""object"",
                                      ""properties"": {
                                        ""name"": {
                                          ""type"": ""string"",
                                          ""pattern"": ""Ed-Fi""
                                        },
                                        ""version"": {
                                          ""type"": ""string""
                                        },
                                        ""informationalVersion"": {
                                          ""type"": ""string""
                                        }
                                      },
                                      ""required"": [
                                        ""name"",
                                        ""version""
                                      ]
                                    }
                                  ]
                                },
                                ""urls"": {
                                  ""type"": ""object"",
                                  ""properties"": {
                                    ""dependencies"": {
                                      ""type"": ""string""
                                    },
                                    ""openApiMetadata"": {
                                      ""type"": ""string""
                                    },
                                    ""oauth"": {
                                      ""type"": ""string""
                                    },
                                    ""dataManagementApi"": {
                                      ""type"": ""string""
                                    },
                                    ""xsdMetadata"": {
                                      ""type"": ""string""
                                    }
                                  },
                                  ""required"": [
                                    ""dependencies"",
                                    ""openApiMetadata"",
                                    ""oauth"",
                                    ""dataManagementApi"",
                                    ""xsdMetadata""
                                  ]
                                }
                              },
                              ""required"": [
                                ""version"",
                                ""informationalVersion"",
                                ""build"",
                                ""apiMode""
                              ]
                         }";

        OdsApiValidatorResult InvalidOdsApiValidatorResult(string exceptionMessage, HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable)
        {
            var message =
                $"Invalid ODS API configured. Please verify that the Production ODS API Url ({apiServerUrl}) is configured correctly.";
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                message += $" Error Details: {exceptionMessage}";
            }

            return new OdsApiValidatorResult
            {
                IsValidOdsApi = false,
                Exception = new OdsApiConnectionException(
                        statusCode, "Invalid ODS API configured.", message)
                { AllowFeedback = false }
            };
        }
    }

    private static JObject ParseJson(string contentAsString)
    {
        return string.IsNullOrEmpty(contentAsString)
            ? new JObject()
            : JObject.Parse(contentAsString);
    }
}
