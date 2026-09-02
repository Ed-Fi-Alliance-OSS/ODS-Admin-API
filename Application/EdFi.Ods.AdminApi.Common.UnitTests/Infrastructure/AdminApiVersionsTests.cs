// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure;

[TestFixture]
public class AdminApiVersionsTests
{
    [Test]
    public void VersionPath_IsDistinctPerVersion_RegardlessOfSharedVersionNumber()
    {
        AdminApiVersions.V1.VersionPath.ShouldBe("v1");
        AdminApiVersions.V2.VersionPath.ShouldBe("v2");
        AdminApiVersions.V3.VersionPath.ShouldBe("v3");
    }
}
