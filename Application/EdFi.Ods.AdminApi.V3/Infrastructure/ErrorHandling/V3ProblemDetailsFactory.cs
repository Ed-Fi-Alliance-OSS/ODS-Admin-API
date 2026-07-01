// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.AspNetCore.Mvc;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

public static class V3ProblemDetailsFactory
{
    public static ProblemDetails Create(
        int status,
        string title,
        string detail,
        string type,
        string? correlationId = null,
        IDictionary<string, object?>? extensions = null
    )
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return problemDetails;
    }

    public static ProblemDetails CreateValidation(
        string detail,
        IDictionary<string, string[]> validationErrors,
        string? correlationId = null
    ) =>
        Create(
            status: StatusCodes.Status400BadRequest,
            title: "Validation failed",
            detail: detail,
            type: AdminApiProblemTypes.BadRequestValidation,
            correlationId: correlationId,
            extensions: new Dictionary<string, object?>
            {
                ["validationErrors"] = validationErrors,
                ["errors"] = validationErrors
            }
        );
}
