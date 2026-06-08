// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using log4net;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

public class V3RequestErrorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private static readonly ILog _logger = LogManager.GetLogger(typeof(V3RequestErrorMiddleware));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(new PathString("/.well-known")))
        {
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

        if (context.Request.Path.StartsWithSegments("/connect/token"))
        {
            await InvokeTokenEndpointAsync(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task InvokeTokenEndpointAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            if (context.Response.StatusCode == 400)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

                if (responseContent.Contains("\"error\": \"invalid_scope\"") ||
                    responseContent.Contains("\"error\":\"invalid_scope\""))
                {
                    context.Response.ContentType = "application/problem+json";
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        catch (Exception ex)
        {
            context.Response.Body = originalBodyStream;
            await HandleExceptionAsync(context, ex);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
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
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        switch (ex)
        {
            case ValidationException:
            case INotFoundException:
                _logger.Debug(
                    JsonSerializer.Serialize(
                        new { message = ex.Message, traceId = context.TraceIdentifier }
                    )
                );
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
                break;
        }

        var (statusCode, problemDetails) = CreateProblemDetails(ex, context.TraceIdentifier);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }

    private static (int StatusCode, Microsoft.AspNetCore.Mvc.ProblemDetails ProblemDetails) CreateProblemDetails(
        Exception exception,
        string correlationId
    )
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                V3ProblemDetailsFactory.CreateValidation(
                    detail: "Validation failed",
                    validationErrors: validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()),
                    correlationId: correlationId
                )
            ),
            INotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                V3ProblemDetailsFactory.Create(
                    status: StatusCodes.Status404NotFound,
                    title: notFoundException.Message,
                    detail: notFoundException.Message,
                    correlationId: correlationId
                )
            ),
            IAdminApiException adminApiException => (
                adminApiException.StatusCode.HasValue ? (int)adminApiException.StatusCode.Value : StatusCodes.Status500InternalServerError,
                V3ProblemDetailsFactory.Create(
                    status: adminApiException.StatusCode.HasValue ? (int)adminApiException.StatusCode.Value : StatusCodes.Status500InternalServerError,
                    title: "Error",
                    detail: string.IsNullOrWhiteSpace(adminApiException.Message)
                        ? "The server encountered an unexpected condition that prevented it from fulfilling the request."
                        : adminApiException.Message,
                    correlationId: correlationId
                )
            ),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                V3ProblemDetailsFactory.Create(
                    status: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "The request body contains malformed JSON. Please ensure your data is properly formatted and try again.",
                    correlationId: correlationId
                )
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                V3ProblemDetailsFactory.Create(
                    status: (int)HttpStatusCode.InternalServerError,
                    title: "Internal Server Error",
                    detail: "The server encountered an unexpected condition that prevented it from fulfilling the request.",
                    correlationId: correlationId
                )
            )
        };
    }
}
