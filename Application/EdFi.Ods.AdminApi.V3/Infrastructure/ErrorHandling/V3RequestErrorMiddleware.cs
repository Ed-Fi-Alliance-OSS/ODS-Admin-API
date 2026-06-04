// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Text.Json;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

public class V3RequestErrorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, problemDetails) = CreateProblemDetails(ex, context.TraceIdentifier);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
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
