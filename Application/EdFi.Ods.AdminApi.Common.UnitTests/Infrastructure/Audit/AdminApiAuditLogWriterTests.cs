// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AdminApiAuditLogWriterTests
{
    private static IConfiguration BuildConfiguration(string databaseEngine = "SqlServer") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:DatabaseEngine"] = databaseEngine
            })
            .Build();

    [Test]
    public void GetOrBuildOptions_CalledTwiceWithSameConnectionString_ReturnsSameCachedInstance()
    {
        var writer = new AdminApiAuditLogWriter(BuildConfiguration());

        var first = writer.GetOrBuildOptions("conn-a");
        var second = writer.GetOrBuildOptions("conn-a");

        second.ShouldBeSameAs(first);
    }

    [Test]
    public void GetOrBuildOptions_CalledWithDifferentConnectionStrings_ReturnsDistinctInstances()
    {
        var writer = new AdminApiAuditLogWriter(BuildConfiguration());

        var forTenantA = writer.GetOrBuildOptions("conn-a");
        var forTenantB = writer.GetOrBuildOptions("conn-b");

        forTenantB.ShouldNotBeSameAs(forTenantA);
    }

    [TestCase("PostgreSql", "Host=localhost;Database=test;Username=u;Password=p", "Npgsql")]
    [TestCase("SqlServer", "Server=localhost;Database=test;User Id=u;Password=p;", "SqlServer")]
    public void GetOrBuildOptions_ConfiguresProviderConnectionStringAndRetry(
        string databaseEngine, string connectionString, string expectedProviderNameFragment)
    {
        var writer = new AdminApiAuditLogWriter(BuildConfiguration(databaseEngine));

        var options = writer.GetOrBuildOptions(connectionString);

        var relationalExtension = options.Extensions.OfType<RelationalOptionsExtension>().Single();
        relationalExtension.GetType().Name.ShouldContain(expectedProviderNameFragment);
        relationalExtension.ConnectionString.ShouldBe(connectionString);

        relationalExtension.ExecutionStrategyFactory.ShouldNotBeNull();
    }
}
