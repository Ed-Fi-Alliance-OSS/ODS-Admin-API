// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class ConnectionStringHelperTests
{
    private const string ValidSqlServerConnectionString =
        "Data Source=localhost;Initial Catalog=EdFi_Ods;Integrated Security=True;";

    private const string ValidPostgreSqlConnectionString =
        "Host=localhost;Database=EdFi_Ods;Username=postgres;Password=secret;";

    [Test]
    public void ValidateConnectionString_WithEmptyEngine_ReturnsFalse()
    {
        var result = ConnectionStringHelper.ValidateConnectionString("", ValidPostgreSqlConnectionString);

        result.ShouldBeFalse();
    }

    [Test]
    public void ValidateConnectionString_WithUnknownEngine_ReturnsFalse()
    {
        var result = ConnectionStringHelper.ValidateConnectionString("Oracle", ValidPostgreSqlConnectionString);

        result.ShouldBeFalse();
    }

    [Test]
    public void ValidateConnectionString_WithSqlServerEngine_AndValidConnectionString_ReturnsTrue()
    {
        var result = ConnectionStringHelper.ValidateConnectionString("SqlServer", ValidSqlServerConnectionString);

        result.ShouldBeTrue();
    }

    [Test]
    public void ValidateConnectionString_WithPostgreSqlEngine_AndValidConnectionString_ReturnsTrue()
    {
        var result = ConnectionStringHelper.ValidateConnectionString("PostgreSql", ValidPostgreSqlConnectionString);

        result.ShouldBeTrue();
    }

    [Test]
    public void ValidateConnectionString_WithSqlServerEngine_AndInvalidConnectionString_ReturnsFalse()
    {
        var result = ConnectionStringHelper.ValidateConnectionString("SqlServer", "not-a-valid-connection-string");

        result.ShouldBeFalse();
    }

    [Test]
    public void ValidateConnectionString_WithPostgreSqlEngine_AndInvalidConnectionString_ReturnsFalse()
    {
        // SqlServer-style keywords are unrecognised by NpgsqlConnectionStringBuilder
        var result = ConnectionStringHelper.ValidateConnectionString("PostgreSql", "Data Source=localhost;Initial Catalog=EdFi_Ods;");

        result.ShouldBeFalse();
    }
}
