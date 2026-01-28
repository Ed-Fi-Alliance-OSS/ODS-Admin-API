// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;
using EdFi.Admin.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Services.Tenants;

[TestFixture]
public class TenantSpecificDbContextProviderTests
{
    private ITenantConfigurationProvider _tenantConfigProvider;
    private IConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _tenantConfigProvider = A.Fake<ITenantConfigurationProvider>();
        _configuration = new ConfigurationBuilder().Build();
    }

    [Test]
    public void GetAdminApiDbContext_ReturnsSqlServerContext_WhenEngineIsSqlServer()
    {
        // Arrange
        var tenantId = "tenant1";
        var config = new TenantConfiguration { TenantIdentifier = tenantId, AdminConnectionString = "Server=(local);Database=TestDb;" };
        A.CallTo(() => _tenantConfigProvider.Get()).Returns(new Dictionary<string, TenantConfiguration> { [tenantId] = config });

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var provider = new TenantSpecificDbContextProvider(_tenantConfigProvider, options, _configuration);

        // Act
        var context = provider.GetAdminApiDbContext(tenantId);

        // Assert
        Assert.That(context, Is.InstanceOf<AdminApiDbContext>());
        context.Database.GetDbConnection().Database.ShouldBe("TestDb");
    }

    [Test]
    public void GetAdminApiDbContext_ReturnsPostgresContext_WhenEngineIsPostgreSql()
    {
        // Arrange
        var tenantId = "tenant2";
        var config = new TenantConfiguration { TenantIdentifier = tenantId, AdminConnectionString = "Host=localhost;Database=TestDb;" };
        A.CallTo(() => _tenantConfigProvider.Get()).Returns(new Dictionary<string, TenantConfiguration> { [tenantId] = config });

        var options = Options.Create(new AppSettings { DatabaseEngine = "PostgreSql" });
        var provider = new TenantSpecificDbContextProvider(_tenantConfigProvider, options, _configuration);

        // Act
        var context = provider.GetAdminApiDbContext(tenantId);

        // Assert
        Assert.That(context, Is.InstanceOf<AdminApiDbContext>());
        context.Database.GetDbConnection().Database.ShouldBe("TestDb");
    }

    [Test]
    public void GetUsersContext_ReturnsSqlServerUsersContext_WhenEngineIsSqlServer()
    {
        // Arrange
        var tenantId = "tenant1";
        var config = new TenantConfiguration { TenantIdentifier = tenantId, AdminConnectionString = "Server=(local);Database=TestDb;" };
        A.CallTo(() => _tenantConfigProvider.Get()).Returns(new Dictionary<string, TenantConfiguration> { [tenantId] = config });

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var provider = new TenantSpecificDbContextProvider(_tenantConfigProvider, options, _configuration);

        // Act
        var context = provider.GetUsersContext(tenantId);

        // Assert
        Assert.That(context, Is.InstanceOf<SqlServerUsersContext>());
    }

    [Test]
    public void GetUsersContext_ReturnsPostgresUsersContext_WhenEngineIsPostgreSql()
    {
        // Arrange
        var tenantId = "tenant2";
        var config = new TenantConfiguration { TenantIdentifier = tenantId, AdminConnectionString = "Host=localhost;Database=TestDb;" };
        A.CallTo(() => _tenantConfigProvider.Get()).Returns(new Dictionary<string, TenantConfiguration> { [tenantId] = config });

        var options = Options.Create(new AppSettings { DatabaseEngine = "PostgreSql" });
        var provider = new TenantSpecificDbContextProvider(_tenantConfigProvider, options, _configuration);

        // Act
        var context = provider.GetUsersContext(tenantId);

        // Assert
        Assert.That(context, Is.InstanceOf<PostgresUsersContext>());
    }

    [Test]
    public void ThrowsNotSupportedException_ForUnknownDatabaseEngine()
    {
        // Arrange
        var tenantId = "tenant1";
        var config = new TenantConfiguration { TenantIdentifier = tenantId, AdminConnectionString = "Server=(local);Database=TestDb;" };
        A.CallTo(() => _tenantConfigProvider.Get()).Returns(new Dictionary<string, TenantConfiguration> { [tenantId] = config });

        var options = Options.Create(new AppSettings { DatabaseEngine = "Oracle" });
        var provider = new TenantSpecificDbContextProvider(_tenantConfigProvider, options, _configuration);

        // Act & Assert
        var ex1 = Should.Throw<NotSupportedException>(() => provider.GetAdminApiDbContext(tenantId));
        ex1.Message.ShouldBe("Database engine 'Oracle' is not supported.");

        var ex2 = Should.Throw<NotSupportedException>(() => provider.GetUsersContext(tenantId));
        ex2.Message.ShouldBe("Database engine 'Oracle' is not supported.");
    }

    [Test]
    public void ThrowsKeyNotFoundException_ForUnknownTenant()
    {
        // Arrange
        var tenantId = "tenant1";
        A.CallTo(() => _tenantConfigProvider.Get()).Returns(new Dictionary<string, TenantConfiguration>());

        var options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        var provider = new TenantSpecificDbContextProvider(_tenantConfigProvider, options, _configuration);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => provider.GetAdminApiDbContext(tenantId));
        Assert.Throws<KeyNotFoundException>(() => provider.GetUsersContext(tenantId));
    }
}
