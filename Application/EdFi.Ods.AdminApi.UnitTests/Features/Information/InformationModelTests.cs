// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Features.Information;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Information;

[TestFixture]
public class InformationModelTests
{
    [Test]
    public void InformationResult_SetProperties_ValuesAreSetCorrectly()
    {
        var version = "1.0.0";
        var build = "1.0.0.0";
        var tenantMode = "singletenant";

        var result = new InformationResult(version, build, tenantMode);

        result.Version.ShouldBe(version);
        result.Build.ShouldBe(build);
        result.TenantMode.ShouldBe(tenantMode);
    }

    [Test]
    public void InformationResult_MultiTenantMode_ValueIsSetCorrectly()
    {
        var version = "2.0.0";
        var build = "2.0.0.0";
        var tenantMode = "multitenant";

        var result = new InformationResult(version, build, tenantMode);

        result.Version.ShouldBe(version);
        result.Build.ShouldBe(build);
        result.TenantMode.ShouldBe(tenantMode);
    }
}
