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
            type: AdminApiProblemTypes.BadRequestVersionMismatch,
            correlationId: "trace-123"
        );

        pd.Detail.ShouldBe("Wrong API version for this instance mode.");
        pd.Type.ShouldBe(AdminApiProblemTypes.BadRequestVersionMismatch);
        pd.Extensions.ShouldNotContainKey("message");
    }

    [Test]
    public void Create_ShouldSetTypeToNotFound()
    {
        var pd = V3ProblemDetailsFactory.Create(
            status: 404,
            title: "Not Found",
            detail: "Resource was not found.",
            type: AdminApiProblemTypes.NotFound,
            correlationId: "trace-456"
        );

        pd.Status.ShouldBe(404);
        pd.Type.ShouldBe(AdminApiProblemTypes.NotFound);
    }

    [Test]
    public void Create_ShouldSetTypeToInternalServerError()
    {
        var pd = V3ProblemDetailsFactory.Create(
            status: 500,
            title: "Internal Server Error",
            detail: "An unexpected error occurred.",
            type: AdminApiProblemTypes.InternalServerError,
            correlationId: "trace-789"
        );

        pd.Status.ShouldBe(500);
        pd.Type.ShouldBe(AdminApiProblemTypes.InternalServerError);
    }

    [Test]
    public void Create_ShouldSetTypeToBadRequestData()
    {
        var pd = V3ProblemDetailsFactory.Create(
            status: 400,
            title: "Bad Request",
            detail: "The request body contains malformed JSON.",
            type: AdminApiProblemTypes.BadRequestData,
            correlationId: "trace-abc"
        );

        pd.Status.ShouldBe(400);
        pd.Type.ShouldBe(AdminApiProblemTypes.BadRequestData);
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
        pd.Type.ShouldBe(AdminApiProblemTypes.BadRequestValidation);
        pd.Extensions.ShouldContainKey("validationErrors");
        pd.Extensions.ShouldContainKey("errors");
        pd.Extensions.ShouldContainKey("correlationId");
        pd.Extensions["errors"].ShouldBe(validationErrors);
    }
}
