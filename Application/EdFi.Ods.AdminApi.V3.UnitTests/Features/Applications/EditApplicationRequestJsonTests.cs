// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications;

[TestFixture]
public class EditApplicationRequestJsonTests
{
    // Mirrors the defaults ASP.NET Core's minimal APIs apply to request-body
    // binding (Microsoft.AspNetCore.Http.Json.JsonOptions) - a bare
    // JsonSerializerOptions() does not enable these, and would misreport
    // ordinary property binding as broken.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const string ValidJson = """
        {
          "id": 1,
          "applicationName": "Test Application",
          "vendorId": 1,
          "claimSetName": "TestClaimSet",
          "educationOrganizationIds": [1]
        }
        """;

    [Test]
    public void Deserialize_WithOnlyKnownProperties_Succeeds()
    {
        var request = JsonSerializer.Deserialize<EditApplication.EditApplicationRequest>(ValidJson, Options);

        request.ShouldNotBeNull();
        request.ApplicationName.ShouldBe("Test Application");
    }

    [Test]
    public void Deserialize_WithEnabled_Throws()
    {
        const string json = """
            {
              "id": 1,
              "applicationName": "Test Application",
              "vendorId": 1,
              "claimSetName": "TestClaimSet",
              "educationOrganizationIds": [1],
              "enabled": false
            }
            """;

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<EditApplication.EditApplicationRequest>(json, Options));
    }

    [Test]
    public void Deserialize_WithDataStoreIds_Throws()
    {
        const string json = """
            {
              "id": 1,
              "applicationName": "Test Application",
              "vendorId": 1,
              "claimSetName": "TestClaimSet",
              "educationOrganizationIds": [1],
              "dataStoreIds": [1]
            }
            """;

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<EditApplication.EditApplicationRequest>(json, Options));
    }
}
