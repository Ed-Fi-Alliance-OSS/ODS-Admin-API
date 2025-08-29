// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure.Services;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Api;

[TestFixture]
public class OdsApiValidatorTests
{
    private const string ValidOdsApiUrl = "https://valid/api";
    private const string InvalidOdsApiUrl = "https://invalid/api";

    private class StubGetRequest : ISimpleGetRequest
    {
        private readonly string _expectedAddress;
        private readonly string _jsonResponse;

        public StubGetRequest(string expectedAddress, string jsonResponse)
        {
            _expectedAddress = expectedAddress;
            _jsonResponse = jsonResponse;
        }

        public Task<string> DownloadString(string address)
        {
            address.ShouldBe(_expectedAddress);
            return Task.FromResult(_jsonResponse);
        }
    }

    private static string ExampleOdsRootDocumentV1()
    {
        return @"{
                      ""version"": ""6.0"",
                      ""informationalVersion"": ""6.0"",
                      ""suite"": ""3"",
                      ""build"": ""6.0.1192.0"",
                      ""apiMode"": ""Sandbox"",
                      ""dataModels"": [
                        {
                          ""name"": ""Ed-Fi"",
                          ""version"": ""4.0.0-a"",
                          ""informationalVersion"": ""The Ed-Fi Data Model 4.0a""
                        },
                        {
                          ""name"": ""TPDM"",
                          ""version"": ""1.1.0"",
                          ""informationalVersion"": ""TPDM-Core""
                        }
                      ],
                      ""urls"": {
                        ""dependencies"": ""https://api.ed-fi.org/v6.0/api/metadata/data/v3/dependencies"",
                        ""openApiMetadata"": ""https://api.ed-fi.org/v6.0/api/metadata/"",
                        ""oauth"": ""https://api.ed-fi.org/v6.0/api/oauth/token"",
                        ""dataManagementApi"": ""https://api.ed-fi.org/v6.0/api/data/v3/"",
                        ""xsdMetadata"": ""https://api.ed-fi.org/v6.0/api/metadata/xsd"",
                        ""changeQueries"": ""https://api.ed-fi.org/v6.0/api/changeQueries/v1/"",
                        ""composites"": ""https://api.ed-fi.org/v6.0/api/composites/v1/"",
                        ""identity"": ""https://api.ed-fi.org/v6.0/api/identity/v2/""
                      }
                    }";
    }

    private static string ExampleOdsRootDocumentV2()
    {
        return @"{
                        ""version"": ""3.2.0"",
                        ""informationalVersion"": ""3.2.0"",
                        ""build"": ""3.2.0.4982"",
                        ""apiMode"": ""Sandbox"",
                        ""dataModels"": [
                            { ""name"": ""Ed-Fi"", ""version"": ""3.1.0"" },
                            { ""name"": ""GrandBend"", ""version"": ""1.0.0"" }
                        ]
                    }";
    }

    private static string ExampleOdsRootDocumentV3()
    {
        return @"{
                    ""version"": ""5.2"",
                    ""informationalVersion"": ""5.2""
                   }";
    }

    private static string ExampleOdsRootDocumentV4()
    {
        return @"{
                    "",""5.2""
                   }";
    }

    private static string ExampleOdsRootDocumentV5()
    {
        return @"{
                        ""version"": ""3.2.0"",
                        ""informationalVersion"": ""3.2.0"",
                        ""build"": ""3.2.0.4982"",
                        ""apiMode"": ""Sandbox"",
                        ""dataModels"": [
                            { ""name"": ""GrandBend"", ""version"": ""1.0.0"" }
                        ]
                    }";
    }

    private static async Task<OdsApiValidatorResult> OdsApiValidationResult(string odsApiUrl, string rootDocument = null)
    {
        var getRootDocument = new StubGetRequest(odsApiUrl, rootDocument);

        var validator = new OdsApiValidator(getRootDocument);

        return await validator.Validate(odsApiUrl);
    }

    [Test]
    public async Task ShouldBeValidForValidRootDocument()
    {
        var rootDocument = ExampleOdsRootDocumentV2();

        var result = await OdsApiValidationResult(ValidOdsApiUrl, rootDocument);

        result.IsValidOdsApi.ShouldBe(true);
        result.Version.ToString().ShouldBe("3.2.0");
    }

    [Test]
    public async Task ShouldBeValidForValidRootDocumentWithMinimumRequiredFields()
    {
        var rootDocument = ExampleOdsRootDocumentV1();

        var result = await OdsApiValidationResult(ValidOdsApiUrl, rootDocument);

        result.IsValidOdsApi.ShouldBe(true);
        result.Version.ToString().ShouldBe("6.0");
    }

    [Test]
    public async Task ShouldBeInvalidForNoRootDocument()
    {
        var result = await OdsApiValidationResult(InvalidOdsApiUrl);

        result.IsValidOdsApi.ShouldBe(false);
        result.Version.ShouldBeNull();
    }

    [Test]
    public async Task ShouldBeInValidForRootDocumentWithMissingFields()
    {
        var rootDocument = ExampleOdsRootDocumentV3();

        var result = await OdsApiValidationResult(InvalidOdsApiUrl, rootDocument);

        result.IsValidOdsApi.ShouldBe(false);
        result.Version.ShouldBeNull();
    }

    [Test]
    public async Task ShouldBeInValidForMalformedRootDocument()
    {
        var rootDocument = ExampleOdsRootDocumentV4();

        var result = await OdsApiValidationResult(InvalidOdsApiUrl, rootDocument);

        result.IsValidOdsApi.ShouldBe(false);
        result.Version.ShouldBeNull();
    }

    [Test]
    public async Task ShouldBeInValidForRootDocumentWithMissingEdFiDataModel()
    {
        var rootDocument = ExampleOdsRootDocumentV5();

        var result = await OdsApiValidationResult(InvalidOdsApiUrl, rootDocument);

        result.IsValidOdsApi.ShouldBe(false);
        result.Version.ShouldBeNull();
    }
}
