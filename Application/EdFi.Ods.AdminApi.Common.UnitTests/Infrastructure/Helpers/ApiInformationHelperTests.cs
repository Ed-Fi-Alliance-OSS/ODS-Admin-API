// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class ApiInformationHelperTests
{
    [TestCase("2.0.1+0a1b2c3", "2.0.1")]
    [TestCase("2.0.1-rc.1+0a1b2c3", "2.0.1-rc.1")]
    [TestCase("2.0.1", "2.0.1")]
    public void NormalizeInformationalVersion_WithMetadataSuffix_StripsBuildMetadata(string raw, string expected)
    {
        var normalized = ApiInformationHelper.NormalizeInformationalVersion(raw, fallbackVersion: "9.9.9");

        normalized.ShouldBe(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void NormalizeInformationalVersion_WithMissingValue_ReturnsFallback(string? raw)
    {
        var normalized = ApiInformationHelper.NormalizeInformationalVersion(raw, fallbackVersion: "9.9.9");

        normalized.ShouldBe("9.9.9");
    }

    [Test]
    public void ApplicationName_IsEdFiOdsAdminApi()
    {
        ApiInformationHelper.ApplicationName.ShouldBe("Ed-Fi ODS Admin API");
    }
}
