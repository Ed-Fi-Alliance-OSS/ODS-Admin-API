// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ErrorHandling;

[TestFixture]
public class V3ProblemDetailsFactoryTests
{
    [Test]
    public void Create_ShouldNotIncludeLegacyMessageExtension()
    {
        var pd = V3ProblemDetailsFactory.Create(
            status: 400,
            title: "Bad Request",
            detail: "Wrong API version for this instance mode.",
            correlationId: "trace-123"
        );

        pd.Detail.ShouldBe("Wrong API version for this instance mode.");
        pd.Extensions.ShouldNotContainKey("message");
    }

    [Test]
    public void CreateValidation_ShouldIncludeBaseMembersAndValidationErrors()
    {
        var validationErrors = new Dictionary<string, string[]> { ["company"] = ["Company is required"] };

        var pd = V3ProblemDetailsFactory.CreateValidation(
            detail: "Validation failed",
            validationErrors: validationErrors,
            correlationId: "trace-123"
        );

        pd.Title.ShouldBe("Validation failed");
        pd.Status.ShouldBe(400);
        pd.Extensions.ShouldContainKey("validationErrors");
        pd.Extensions.ShouldContainKey("errors");
        pd.Extensions.ShouldContainKey("correlationId");
        pd.Extensions["errors"].ShouldBe(validationErrors);
    }
}
