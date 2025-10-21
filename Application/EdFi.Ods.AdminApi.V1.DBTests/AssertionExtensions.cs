// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Shouldly;
using static System.Environment;

namespace EdFi.Ods.AdminApi.V1.DBTests;

public static class AssertionExtensions
{
    public static void ShouldValidate<TModel>(this AbstractValidator<TModel> validator, TModel model)
        => validator.Validate(model).ShouldBeSuccessful();

    public static void ShouldNotValidate<TModel>(this AbstractValidator<TModel> validator, TModel model, params string[] expectedErrors)
        => validator.Validate(model).ShouldBeFailure(expectedErrors);

    private static void ShouldBeSuccessful(this ValidationResult result)
    {
        var indentedErrorMessages = result
            .Errors
            .OrderBy(x => x.ErrorMessage)
            .Select(x => "    " + x.ErrorMessage)
            .ToArray();

        var actual = string.Join(NewLine, indentedErrorMessages);

        result.IsValid.ShouldBeTrue($"Expected no validation errors, but found {result.Errors.Count}:{NewLine}{actual}");
    }

    private static void ShouldBeFailure(this ValidationResult result, params string[] expectedErrors)
    {
        result.IsValid.ShouldBeFalse("Expected validation errors, but the message passed validation.");

        result.Errors
            .OrderBy(x => x.ErrorMessage)
            .Select(x => x.ErrorMessage)
            .ToArray()
            .ShouldBe([.. expectedErrors.OrderBy(x => x)]);
    }

    public static void ShouldSatisfy<T>(this IEnumerable<T> actual, params Action<T>[] itemExpectations)
    {
        var actualItems = actual.ToArray();

        if (actualItems.Length != itemExpectations.Length)
            throw new Exception(
                $"Expected the collection to have {itemExpectations.Length} " +
                $"items, but there were {actualItems.Length} items.");

        for (var i = 0; i < actualItems.Length; i++)
        {
            try
            {
                itemExpectations[i](actualItems[i]);
            }
            catch (Exception failure)
            {
                throw new Exception($"Assertion failed for item at position [{i}].", failure);
            }
        }
    }
}
