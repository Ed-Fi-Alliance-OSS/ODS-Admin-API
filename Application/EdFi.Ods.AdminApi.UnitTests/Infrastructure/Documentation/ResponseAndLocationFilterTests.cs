// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using NUnit.Framework;
using Shouldly;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Documentation;

[TestFixture]
public class ResponseAndLocationFilterTests
{
    [Test]
    public void Should_add_the_declared_example_to_the_json_content()
    {
        var operation = Operation202WithJsonContent();
        var context = Context("POST", new ResponseExampleMetadata(202, """{"jobId":"j-1","message":"queued"}"""));

        new ResponseExampleOperationFilter().Apply(operation, context);

        var example = operation.Responses!["202"].Content!["application/json"].Example;
        example.ShouldNotBeNull();
        example["jobId"]!.GetValue<string>().ShouldBe("j-1");
        example["message"]!.GetValue<string>().ShouldBe("queued");
    }

    [Test]
    public void Should_not_add_an_example_when_none_is_declared()
    {
        var operation = Operation202WithJsonContent();

        new ResponseExampleOperationFilter().Apply(operation, Context("POST"));

        operation.Responses!["202"].Content!["application/json"].Example.ShouldBeNull();
    }

    [Test]
    public void Should_add_location_header_to_a_202_post()
    {
        var operation = Operation202WithJsonContent();

        new LocationHeaderOperationFilter().Apply(operation, Context("POST"));

        operation.Responses!["202"].Headers!.ShouldContainKey("Location");
    }

    [Test]
    public void Should_not_add_location_header_to_a_delete()
    {
        var operation = Operation202WithJsonContent();

        new LocationHeaderOperationFilter().Apply(operation, Context("DELETE"));

        (operation.Responses!["202"].Headers?.Count ?? 0).ShouldBe(0);
    }

    private static OpenApiOperation Operation202WithJsonContent() =>
        new()
        {
            Responses = new OpenApiResponses
            {
                ["202"] = new OpenApiResponse
                {
                    Description = "Accepted",
                    Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() }
                }
            }
        };

    private static OperationFilterContext Context(string httpMethod, params object[] endpointMetadata) =>
        new(
            new ApiDescription
            {
                HttpMethod = httpMethod,
                ActionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata }
            },
            A.Fake<ISchemaGenerator>(),
            new SchemaRepository(),
            new OpenApiDocument(),
            methodInfo: null);
}
