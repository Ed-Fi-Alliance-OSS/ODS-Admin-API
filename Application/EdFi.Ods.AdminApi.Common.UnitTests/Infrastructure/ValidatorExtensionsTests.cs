// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using FluentValidation;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure;

[TestFixture]
public class ValidatorExtensionsTests
{
    [Test]
    public void GuardRouteIdMatchesBodyId_WhenIdsMatch_ShouldNotThrow()
    {
        Should.NotThrow(() => ValidatorExtensions.GuardRouteIdMatchesBodyId(10, 10, "Id"));
    }

    [Test]
    public void GuardRouteIdMatchesBodyId_WhenBodyIdIsZeroAndRouteIdDiffers_ShouldThrowValidationException()
    {
        var exception = Should.Throw<ValidationException>(() =>
            ValidatorExtensions.GuardRouteIdMatchesBodyId(10, 0, "Id"));

        exception.Errors.Single(x => x.PropertyName == "Id").ErrorMessage
            .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
    }

    [Test]
    public void GuardRouteIdMatchesBodyId_WhenRouteIdIsZeroAndBodyIdDiffers_ShouldThrowValidationException()
    {
        var exception = Should.Throw<ValidationException>(() =>
            ValidatorExtensions.GuardRouteIdMatchesBodyId(0, 10, "Id"));

        exception.Errors.Single(x => x.PropertyName == "Id").ErrorMessage
            .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
    }
}
