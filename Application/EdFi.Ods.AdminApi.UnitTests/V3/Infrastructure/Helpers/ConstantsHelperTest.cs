// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V3.Infrastructure.Helpers;

[TestFixture]
public class ConstantsHelperTest
{
    [TestCase("2.0.1+0a1b2c3", "2.0.1")]
    [TestCase("2.0.1-rc.1+0a1b2c3", "2.0.1-rc.1")]
    [TestCase("2.0.1", "2.0.1")]
    public void Given_InformationalVersionWithMetadata_When_Normalizing_Then_StripsBuildMetadata(
        string raw,
        string expected)
    {
        var normalized = ConstantsHelpers.NormalizeInformationalVersion(raw);

        normalized.ShouldBe(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Given_MissingInformationalVersion_When_Normalizing_Then_ReturnsReleaseFallback(string raw)
    {
        var normalized = ConstantsHelpers.NormalizeInformationalVersion(raw);

        normalized.ShouldBe(ConstantsHelpers.Version);
    }

    [Test]
    public void ApplicationName_IsEdFiAdminApi()
    {
        ConstantsHelpers.ApplicationName.ShouldBe("Ed-Fi Admin API");
    }
}
