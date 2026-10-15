// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using NUnit.Framework;
using Shouldly;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Documentation;

[TestFixture]
public class V2ErrorResponseOperationFilterTests
{
    private const string ProblemJson = "application/problem+json";

    private readonly V2ErrorResponseOperationFilter _filter = new();
    private SchemaRepository _schemaRepository = null!;

    [SetUp]
    public void SetUp() => _schemaRepository = new SchemaRepository();

    [Test]
    public void Should_not_add_a_body_to_401_and_403()
    {
        var operation = Apply("v2/vendors", "401", "403");

        (operation.Responses!["401"].Content?.Count ?? 0).ShouldBe(0);
        (operation.Responses["403"].Content?.Count ?? 0).ShouldBe(0);
        _schemaRepository.Schemas.ShouldBeEmpty();
    }

    [Test]
    public void Should_document_400_as_validation_error_or_message()
    {
        var operation = Apply("v2/vendors", "400");

        var schema = Schema(operation, "400", ProblemJson).ShouldBeOfType<OpenApiSchema>();
        schema.OneOf!.Select(Id).ShouldBe(["validationErrorResult", "messageResult"]);
    }

    [TestCase("404", "notFoundResult")]
    [TestCase("409", "messageResult")]
    [TestCase("500", "messageResult")]
    [TestCase("503", "messageResult")]
    public void Should_document_error_status_codes_with_legacy_schema(string statusCode, string schemaId)
    {
        var operation = Apply("v2/vendors/{id}", statusCode);

        Id(Schema(operation, statusCode, ProblemJson)).ShouldBe(schemaId);
        _schemaRepository.Schemas.ShouldContainKey(schemaId);
    }

    [Test]
    public void Should_document_token_400_as_oauth_error_or_validation_error_for_problem_json_and_oauth_error_for_json()
    {
        var operation = Apply("connect/token", "400");

        var schema = Schema(operation, "400", ProblemJson).ShouldBeOfType<OpenApiSchema>();
        schema.OneOf!.Select(Id).ShouldBe(["oauthErrorResult", "validationErrorResult"]);
        Id(Schema(operation, "400", "application/json")).ShouldBe("oauthErrorResult");
    }

    [Test]
    public void Should_document_register_400_as_validation_error()
    {
        var operation = Apply("connect/register", "400");

        Id(Schema(operation, "400", ProblemJson)).ShouldBe("validationErrorResult");
    }

    [Test]
    public void Should_not_overwrite_existing_content()
    {
        var existing = new OpenApiSchema();
        var operation = Operation("500");
        ((OpenApiResponse)operation.Responses!["500"]).Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new() { Schema = existing }
        };

        _filter.Apply(operation, Context("v2/vendors"));

        Schema(operation, "500", "application/json").ShouldBeSameAs(existing);
        operation.Responses["500"].Content!.ShouldNotContainKey(ProblemJson);
    }

    [Test]
    public void Should_register_schemas_that_match_the_published_contract()
    {
        Apply("v2/vendors", "400", "404", "500");
        Apply("connect/token", "400");

        _schemaRepository.Schemas.Keys.OrderBy(x => x).ShouldBe(
            ["messageResult", "notFoundResult", "oauthErrorResult", "validationErrorResult"]);

        var message = _schemaRepository.Schemas["messageResult"].ShouldBeOfType<OpenApiSchema>();
        message.Required.ShouldBe(["message"]);
        message.AdditionalPropertiesAllowed.ShouldBeFalse();
    }

    private OpenApiOperation Apply(string relativePath, params string[] statusCodes)
    {
        var operation = Operation(statusCodes);
        _filter.Apply(operation, Context(relativePath));
        return operation;
    }

    private static OpenApiOperation Operation(params string[] statusCodes)
    {
        var responses = new OpenApiResponses();
        foreach (var code in statusCodes)
            responses[code] = new OpenApiResponse { Description = code };

        return new OpenApiOperation { Responses = responses };
    }

    private OperationFilterContext Context(string relativePath) =>
        new(
            new ApiDescription { RelativePath = relativePath },
            A.Fake<ISchemaGenerator>(),
            _schemaRepository,
            new OpenApiDocument(),
            methodInfo: null);

    private static IOpenApiSchema Schema(OpenApiOperation operation, string statusCode, string contentType) =>
        operation.Responses![statusCode].Content![contentType].Schema!;

    private static string Id(IOpenApiSchema schema) => schema.ShouldBeOfType<OpenApiSchemaReference>().Reference.Id!;
}
