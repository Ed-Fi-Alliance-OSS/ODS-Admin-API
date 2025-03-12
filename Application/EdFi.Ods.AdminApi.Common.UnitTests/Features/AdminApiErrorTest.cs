// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;
using Results = FluentValidation.Results;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Features;

[TestFixture]
public class AdminApiErrorTest
{
    [Test]
    public void Validation_ShouldReturnValidationProblem()
    {
        // Arrange
        var validationFailures = new List<Results.ValidationFailure>
        {
            new("Property1", "Error1"),
            new("Property1", "Error2"),
            new("Property2", "Error3")
        };

        // Act
        var result = AdminApiError.Validation(validationFailures);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
    }

    [Test]
    public void Validation_ShouldNotBeNull()
    {
        // Arrange
        var validationFailures = new List<Results.ValidationFailure>
        {
            new("Property1", "Error1"),
            new("Property1", "Error2"),
            new("Property2", "Error3")
        };

        // Act
        var result = AdminApiError.Validation(validationFailures);

        // Assert
        var validationProblemDetails = result as ProblemHttpResult;
        validationProblemDetails.ShouldNotBeNull();
    }

    [Test]
    public void Validation_ShouldContainErrors()
    {
        // Arrange
        var validationFailures = new List<Results.ValidationFailure>
        {
            new("Property1", "Error1"),
            new("Property1", "Error2"),
            new("Property2", "Error3")
        };

        // Act
        var result = AdminApiError.Validation(validationFailures);

        // Assert
        var details = (result as ProblemHttpResult)?.ProblemDetails as HttpValidationProblemDetails;
        details.ShouldNotBeNull();
        details.Errors.ShouldContainKey("Property1");
        details.Errors["Property1"].ShouldBeEquivalentTo(new[] { "Error1", "Error2" });
        details.Errors.ShouldContainKey("Property2");
        details.Errors["Property2"].ShouldBeEquivalentTo(new[] { "Error3" });
    }

    [Test]
    public void Unexpected_ShouldReturnProblemWithMessage()
    {
        // Arrange
        var message = "Unexpected error occurred";

        // Act
        var result = AdminApiError.Unexpected(message);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
    }

    [Test]
    public void Unexpected_ShouldNotBeNull()
    {
        // Arrange
        var message = "Unexpected error occurred";

        // Act
        var result = AdminApiError.Unexpected(message);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails.ShouldNotBeNull();
    }

    [Test]
    public void Unexpected_ShouldHaveCorrectTitle()
    {
        // Arrange
        var message = "Unexpected error occurred";

        // Act
        var result = AdminApiError.Unexpected(message);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Title.ShouldBe(message);
    }

    [Test]
    public void Unexpected_ShouldHaveCorrectStatus()
    {
        // Arrange
        var message = "Unexpected error occurred";

        // Act
        var result = AdminApiError.Unexpected(message);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Status.ShouldBe(500);
    }

    [Test]
    public void Unexpected_WithErrors_ShouldReturnProblemWithMessageAndErrors()
    {
        // Arrange
        var message = "Unexpected error occurred";
        var errors = new[] { "Error1", "Error2" };

        // Act
        var result = AdminApiError.Unexpected(message, errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
    }

    [Test]
    public void Unexpected_WithErrors_ShouldNotBeNull()
    {
        // Arrange
        var message = "Unexpected error occurred";
        var errors = new[] { "Error1", "Error2" };

        // Act
        var result = AdminApiError.Unexpected(message, errors);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails.ShouldNotBeNull();
    }

    [Test]
    public void Unexpected_WithErrors_ShouldHaveCorrectTitle()
    {
        // Arrange
        var message = "Unexpected error occurred";
        var errors = new[] { "Error1", "Error2" };

        // Act
        var result = AdminApiError.Unexpected(message, errors);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Title.ShouldBe(message);
    }

    [Test]
    public void Unexpected_WithErrors_ShouldHaveCorrectStatus()
    {
        // Arrange
        var message = "Unexpected error occurred";
        var errors = new[] { "Error1", "Error2" };

        // Act
        var result = AdminApiError.Unexpected(message, errors);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Status.ShouldBe(500);
    }

    [Test]
    public void Unexpected_WithErrors_ShouldHaveCorrectExtensions()
    {
        // Arrange
        var message = "Unexpected error occurred";
        var errors = new[] { "Error1", "Error2" };

        // Act
        var result = AdminApiError.Unexpected(message, errors);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Extensions["errors"].ShouldBeEquivalentTo(errors);
    }

    [Test]
    public void Unexpected_WithException_ShouldReturnProblemWithExceptionMessage()
    {
        // Arrange
        var exception = new Exception("Exception message");

        // Act
        var result = AdminApiError.Unexpected(exception);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
    }

    [Test]
    public void Unexpected_WithException_ShouldNotBeNull()
    {
        // Arrange
        var exception = new Exception("Exception message");

        // Act
        var result = AdminApiError.Unexpected(exception);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails.ShouldNotBeNull();
    }

    [Test]
    public void Unexpected_WithException_ShouldHaveCorrectTitle()
    {
        // Arrange
        var exception = new Exception("Exception message");

        // Act
        var result = AdminApiError.Unexpected(exception);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Title.ShouldBe(exception.Message);
    }

    [Test]
    public void Unexpected_WithException_ShouldHaveCorrectStatus()
    {
        // Arrange
        var exception = new Exception("Exception message");

        // Act
        var result = AdminApiError.Unexpected(exception);

        // Assert
        var problemDetails = result as ProblemHttpResult;
        problemDetails?.ProblemDetails.Status.ShouldBe(500);
    }

    [Test]
    public void NotFound_ShouldReturnNotFoundWithResourceNameAndId()
    {
        // Arrange
        var resourceName = "Resource";
        var id = 123;

        // Act
        var result = AdminApiError.NotFound(resourceName, id);

        // Assert
        result.ShouldBeOfType<NotFound<string>>();
    }

    [Test]
    public void NotFound_ShouldNotBeNull()
    {
        // Arrange
        var resourceName = "Resource";
        var id = 123;

        // Act
        var result = AdminApiError.NotFound(resourceName, id);

        // Assert
        var notFoundResult = result as NotFound<string>;
        notFoundResult.ShouldNotBeNull();
    }

    [Test]
    public void NotFound_ShouldHaveCorrectValue()
    {
        // Arrange
        var resourceName = "Resource";
        var id = 123;

        // Act
        var result = AdminApiError.NotFound(resourceName, id);

        // Assert
        var notFoundResult = result as NotFound<string>;
        notFoundResult?.Value.ShouldBe($"Not found: {resourceName} with ID {id}");
    }

    [Test]
    public void Validation_WithEmptyErrors_ShouldReturnValidationProblem()
    {
        // Act
        var result = AdminApiError.Validation(new List<Results.ValidationFailure>());

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
    }

    [Test]
    public void Validation_WithEmptyErrors_ShouldNotBeNull()
    {
        // Act
        var result = AdminApiError.Validation(new List<Results.ValidationFailure>());

        // Assert
        var validationProblemDetails = result as ProblemHttpResult;
        validationProblemDetails.ShouldNotBeNull();
    }

    [Test]
    public void Validation_WithEmptyErrors_ShouldHaveEmptyErrors()
    {
        // Act
        var result = AdminApiError.Validation(new List<Results.ValidationFailure>());

        // Assert
        var details = (result as ProblemHttpResult)?.ProblemDetails as HttpValidationProblemDetails;
        details.ShouldNotBeNull();
        details.Errors.ShouldBeEmpty();
    }
}
