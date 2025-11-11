// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Text.Json;
using EdFi.Common.Utils.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using log4net;

namespace EdFi.Ods.AdminApi.Features;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ILog _logger = LogManager.GetLogger(typeof(RequestLoggingMiddleware));

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            if (context.Request.Path.StartsWithSegments(new PathString("/.well-known")))
            {
                // Requests to the OpenId Connect ".well-known" endpoint are too chatty for informational logging, but could be useful in debug logging.
                _logger.Debug(
                    JsonSerializer.Serialize(
                        new { path = context.Request.Path.Value, traceId = context.TraceIdentifier }
                    )
                );
            }
            else
            {
                _logger.Info(
                    JsonSerializer.Serialize(
                        new { path = context.Request.Path.Value, traceId = context.TraceIdentifier }
                    )
                );
            }

            // Check if this is a token endpoint request and intercept the response
            if (context.Request.Path.StartsWithSegments("/connect/token"))
            {
                // Capture the original response body stream
                var originalBodyStream = context.Response.Body;

                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                try
                {
                    // Execute the next middleware
                    await _next(context);

                    // Check if response is 400 and contains invalid_scope error
                    if (context.Response.StatusCode == 400)
                    {
                        responseBody.Seek(0, SeekOrigin.Begin);
                        var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

                        // Check if the response contains invalid_scope error
                        if (responseContent.Contains("\"error\": \"invalid_scope\""))
                        {
                            context.Response.ContentType = "application/problem+json";
                        }

                        // Write the response back to the original stream
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                    }
                    else
                    {
                        // For non-400 responses, just copy the response back
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                    }
                }
                finally
                {
                    // Restore the original response body stream
                    context.Response.Body = originalBodyStream;
                }
            }
            else
            {
                await _next(context);
            }
        }
        catch (Exception ex)
        {
            var response = context.Response;

            // Check if response has already started or stream is closed
            if (response.HasStarted)
            {
                _logger.Error(
                    JsonSerializer.Serialize(
                        new
                        {
                            message = "Cannot write to response, response has already started",
                            error = new { ex.Message, ex.StackTrace },
                            traceId = context.TraceIdentifier
                        }
                    ),
                    ex
                );
                return;
            }

            response.ContentType = "application/problem+json";

            switch (ex)
            {
                case ValidationException validationException:
                    var validationResponse = new
                    {
                        title = "Validation failed",
                        errors = new Dictionary<string, List<string>>()
                    };

                    validationException.Errors.ForEach(x =>
                    {
                        if (!validationResponse.errors.ContainsKey(x.PropertyName))
                        {
                            validationResponse.errors[x.PropertyName] = new List<string>();
                        }
                        validationResponse.errors[x.PropertyName].Add(x.ErrorMessage.Replace("\u0027", "'"));
                    });

#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    _logger.Debug(
                        JsonSerializer.Serialize(
                            new { message = validationResponse, traceId = context.TraceIdentifier }
                        )
                    );
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.

                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await response.WriteAsync(JsonSerializer.Serialize(validationResponse));
                    break;

                case INotFoundException notFoundException:
                    var notFoundResponse = new { title = notFoundException.Message, };
                    _logger.Debug(
                        JsonSerializer.Serialize(
                            new { message = notFoundResponse, traceId = context.TraceIdentifier }
                        )
                    );

                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    await response.WriteAsync(JsonSerializer.Serialize(notFoundResponse));
                    break;

                case IAdminApiException adminApiException:
                    var message =
                        adminApiException.StatusCode.HasValue
                        && !string.IsNullOrWhiteSpace(adminApiException.Message)
                            ? adminApiException.Message
                            : "The server encountered an unexpected condition that prevented it from fulfilling the request.";
                    _logger.Error(
                        JsonSerializer.Serialize(
                            new
                            {
                                message = "An uncaught error has occurred",
                                error = new { ex.Message, ex.StackTrace },
                                traceId = context.TraceIdentifier
                            }
                        ),
                        ex
                    );
                    response.StatusCode = adminApiException.StatusCode.HasValue
                        ? (int)adminApiException.StatusCode
                        : 500;
                    await response.WriteAsync(JsonSerializer.Serialize(new { message = message }));
                    break;

                case BadHttpRequestException:
                    _logger.Error(
                        JsonSerializer.Serialize(
                            new
                            {
                                message = "The request body contains malformed JSON. Please ensure your data is properly formatted and try again.",
                                error = new { ex.Message, ex.StackTrace },
                                traceId = context.TraceIdentifier
                            }
                        ),
                        ex
                    );
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await response.WriteAsync(JsonSerializer.Serialize(new { message = "The request body contains malformed JSON. Please ensure your data is properly formatted and try again." }));
                    break;

                default:
                    _logger.Error(
                        JsonSerializer.Serialize(
                            new
                            {
                                message = "An uncaught error has occurred",
                                error = new { ex.Message, ex.StackTrace },
                                traceId = context.TraceIdentifier
                            }
                        ),
                        ex
                    );
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await response.WriteAsync(
                        JsonSerializer.Serialize(
                            new
                            {
                                message = "The server encountered an unexpected condition that prevented it from fulfilling the request."
                            }
                        )
                    );
                    break;
            }
        }
    }
}
