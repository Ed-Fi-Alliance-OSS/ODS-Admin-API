// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using FluentValidation.Results;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Features;

[SwaggerSchema(Title = "AdminApiError", Description = "Wrapper schema for all error responses")]
public class AdminApiError
{
    protected AdminApiError(int status, string title)
    {
        Status = status;
        Title = title;
        Errors = new[] { title };
    }

    protected AdminApiError(int status, string title, IEnumerable<string> errors) : this(status, title)
    {
        Errors = errors;
    }

    public int Status { get; }
    public string Title { get; }
    public IEnumerable<string> Errors { get; }

    public static IResult Validation(IEnumerable<ValidationFailure> errors)
        => Results.ValidationProblem(errors
            .GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()));

    public static IResult Unexpected(string message)
        => Results.Problem(statusCode: 500, title: message);

    public static IResult Unexpected(string message, IEnumerable<string> errors)
        => Results.Problem(statusCode: 500, title: message, extensions: new Dictionary<string, object?> { { "errors", errors } });

    public static IResult Unexpected(Exception exception)
        => Results.Problem(statusCode: 500, title: exception.Message);

    public static IResult NotFound<T>(string resourceName, T id)
        => Results.NotFound(new AdminApiError(404, $"Not found: {resourceName} with ID {id}"));
}
