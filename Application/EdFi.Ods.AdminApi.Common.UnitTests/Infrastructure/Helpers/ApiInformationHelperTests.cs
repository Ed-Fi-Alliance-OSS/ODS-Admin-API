// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class ApiInformationHelperTests
{
    [TestCase("2.0.1+0a1b2c3", "2.0.1")]
    [TestCase("2.0.1-rc.1+0a1b2c3", "2.0.1-rc.1")]
    [TestCase("2.0.1", "2.0.1")]
    [TestCase("2.0.1+0a1b2c3+extra", "2.0.1")]
    public void NormalizeInformationalVersion_WithMetadataSuffix_StripsBuildMetadata(string raw, string expected)
    {
        var normalized = ApiInformationHelper.NormalizeInformationalVersion(raw, fallbackVersion: "9.9.9");

        normalized.ShouldBe(expected);
    }

    [TestCase("+0a1b2c3")]
    [TestCase("+")]
    public void NormalizeInformationalVersion_WithMetadataMarkerAtStart_ReturnsFallback(string raw)
    {
        var normalized = ApiInformationHelper.NormalizeInformationalVersion(raw, fallbackVersion: "9.9.9");

        normalized.ShouldBe("9.9.9");
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

    [Test]
    public void FormatVersion_WithFourPartVersion_DropsRevisionSegment()
    {
        var formatted = ApiInformationHelper.FormatVersion(new Version(2, 4, 0, 0));

        formatted.ShouldBe("2.4.0");
    }

    [Test]
    public void FormatVersion_WithNullVersion_ReturnsZeroFallback()
    {
        var formatted = ApiInformationHelper.FormatVersion(null);

        formatted.ShouldBe("0.0.0");
    }

    [Test]
    public void Version_And_Build_AreDerivedFromTheSameAssemblyVersion()
    {
        // Version drops the 4th (revision) segment that Build carries, so the two must always
        // share the same major.minor.build prefix rather than being stamped independently.
        ApiInformationHelper.Build.ShouldStartWith(ApiInformationHelper.Version);
    }
}
