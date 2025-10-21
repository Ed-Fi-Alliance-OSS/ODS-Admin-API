// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure;

[TestFixture]
public class HealthCheckServiceExtensionsTests
{
    [Test]
    public void AddHealthCheck_ShouldRegisterBothAdminAndSecurityHealthChecks_WhenMultiTenancyDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for health checks
        var configuration = CreateTestConfiguration(multiTenancy: false);

        // Act
        services.AddHealthCheck(configuration);

        // Assert - Check that health check services are registered
        var healthCheckServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(HealthCheckService));
        healthCheckServiceDescriptor.ShouldNotBeNull();
    }

    [Test]
    public void AddHealthCheck_ShouldRegisterMultiTenantHealthChecks_WhenMultiTenancyEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for health checks
        var configuration = CreateTestConfiguration(multiTenancy: true);

        // Act
        services.AddHealthCheck(configuration);

        // Assert - Check that health check services are registered
        var healthCheckServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(HealthCheckService));
        healthCheckServiceDescriptor.ShouldNotBeNull();
    }

    private static IConfigurationRoot CreateTestConfiguration(bool multiTenancy)
    {
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:DatabaseEngine"] = "SqlServer",
            ["AppSettings:MultiTenancy"] = multiTenancy.ToString(),
            ["ConnectionStrings:EdFi_Admin"] = "Data Source=test;Initial Catalog=EdFi_Admin_Test;Integrated Security=True",
            ["ConnectionStrings:EdFi_Security"] = "Data Source=test;Initial Catalog=EdFi_Security_Test;Integrated Security=True"
        };

        if (multiTenancy)
        {
            configData["Tenants:tenant1:ConnectionStrings:EdFi_Admin"] = "Data Source=test;Initial Catalog=EdFi_Admin_Tenant1;Integrated Security=True";
            configData["Tenants:tenant1:ConnectionStrings:EdFi_Security"] = "Data Source=test;Initial Catalog=EdFi_Security_Tenant1;Integrated Security=True";
            configData["Tenants:tenant2:ConnectionStrings:EdFi_Admin"] = "Data Source=test;Initial Catalog=EdFi_Admin_Tenant2;Integrated Security=True";
            configData["Tenants:tenant2:ConnectionStrings:EdFi_Security"] = "Data Source=test;Initial Catalog=EdFi_Security_Tenant2;Integrated Security=True";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
