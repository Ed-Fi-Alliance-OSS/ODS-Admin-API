// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Common.Features;

[SwaggerSchema(Title = "AdminApiError", Description = "Wrapper schema for all error responses")]
public static class AdminApiError
{
    public static IResult Validation(IEnumerable<ValidationFailure> errors) =>
        Results.ValidationProblem(
            errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray())
        );

    public static IResult Unexpected(string message) => Results.Problem(statusCode: 500, title: message);

    public static IResult Unexpected(string message, IEnumerable<string> errors) =>
        Results.Problem(
            statusCode: 500,
            title: message,
            extensions: new Dictionary<string, object?> { { "errors", errors } }
        );

    public static IResult Unexpected(Exception exception) =>
        Results.Problem(statusCode: 500, title: exception.Message);

    public static IResult NotFound<T>(string resourceName, T id) =>
        Results.NotFound($"Not found: {resourceName} with ID {id}");
}
