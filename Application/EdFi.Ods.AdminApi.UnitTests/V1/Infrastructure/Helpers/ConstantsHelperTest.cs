// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V1.Infrastructure.Helpers;

[TestFixture]
public class ConstantsHelperTest
{
    [Test]
    public void ApplicationName_IsEdFiOdsAdminApi()
    {
        ConstantsHelpers.ApplicationName.ShouldBe("Ed-Fi ODS Admin API");
    }
}
