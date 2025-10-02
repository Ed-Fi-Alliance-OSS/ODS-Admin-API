// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Features.Tenants;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Tenants;

[TestFixture]
public class TenantModelTests
{
    [Test]
    public void DefaultConstructor_ShouldInitializePropertiesToEmptyStrings()
    {
        // Act
        var connectionStrings = new TenantModelConnectionStrings();

        // Assert
        connectionStrings.EdFiAdminConnectionString.ShouldBe(string.Empty);
        connectionStrings.EdFiSecurityConnectionString.ShouldBe(string.Empty);
    }

    [Test]
    public void ParameterizedConstructor_ShouldSetProperties()
    {
        // Arrange
        var adminConn = "AdminConn";
        var securityConn = "SecurityConn";

        // Act
        var connectionStrings = new TenantModelConnectionStrings(adminConn, securityConn);

        // Assert
        connectionStrings.EdFiAdminConnectionString.ShouldBe(adminConn);
        connectionStrings.EdFiSecurityConnectionString.ShouldBe(securityConn);
    }

    [Test]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var connectionStrings = new TenantModelConnectionStrings
        {
            // Act
            EdFiAdminConnectionString = "NewAdmin",
            EdFiSecurityConnectionString = "NewSecurity"
        };

        // Assert
        connectionStrings.EdFiAdminConnectionString.ShouldBe("NewAdmin");
        connectionStrings.EdFiSecurityConnectionString.ShouldBe("NewSecurity");
    }
}
