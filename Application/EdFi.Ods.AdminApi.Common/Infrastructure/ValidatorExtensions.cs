// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using FluentValidation;
using FluentValidation.Results;

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

public static class ValidatorExtensions
{
    public static async Task GuardAsync<TRequest>(this IValidator<TRequest> validator, TRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
    }

    public static void GuardRouteIdMatchesBodyId(int routeId, int requestId, string propertyName)
    {
        if (routeId > 0 && routeId != requestId)
        {
            throw new ValidationException([new ValidationFailure(propertyName, ErrorMessagesConstants.RequestBodyIdMismatch)]);
        }
    }
}
