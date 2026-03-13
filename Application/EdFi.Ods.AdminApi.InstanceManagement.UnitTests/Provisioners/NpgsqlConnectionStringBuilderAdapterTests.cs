// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using Shouldly;

namespace EdFi.Ods.AdminApi.InstanceManagement.UnitTests.Provisioners;

[TestFixture]
public class NpgsqlConnectionStringBuilderAdapterTests
{
    [Test]
    public void ConnectionString_Get_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter();

        var ex = Should.Throw<InvalidOperationException>(() => _ = sut.ConnectionString);

        ex.Message.ShouldBe("Connection string has not been set.");
    }

    [Test]
    public void ConnectionString_Set_ShouldExposeTheConfiguredConnectionString()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter();

        sut.ConnectionString = "Host=localhost;Database=EdFi_Ods;Username=postgres;Password=secret";

        sut.ConnectionString.ShouldContain("Host=localhost");
        sut.ConnectionString.ShouldContain("Database=EdFi_Ods");
    }

    [Test]
    public void DatabaseName_Get_WhenConnectionStringIsSet_ShouldReturnDatabaseName()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter
        {
            ConnectionString = "Host=localhost;Database=EdFi_Ods;Username=postgres;Password=secret"
        };

        sut.DatabaseName.ShouldBe("EdFi_Ods");
    }

    [Test]
    public void DatabaseName_Set_WhenConnectionStringIsSet_ShouldUpdateDatabaseName()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter
        {
            ConnectionString = "Host=localhost;Database=EdFi_Ods;Username=postgres;Password=secret"
        };

        sut.DatabaseName = "Ods_Empty_Template";

        sut.DatabaseName.ShouldBe("Ods_Empty_Template");
        sut.ConnectionString.ShouldContain("Database=Ods_Empty_Template");
    }

    [Test]
    public void DatabaseName_Set_WhenConnectionStringIsNotSet_ShouldThrowInvalidOperationException()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter();

        var ex = Should.Throw<InvalidOperationException>(() => sut.DatabaseName = "EdFi_Ods");

        ex.Message.ShouldBe("Connection string has not been set.");
    }

    [Test]
    public void ServerName_GetAndSet_WhenConnectionStringIsSet_ShouldReadAndUpdateHost()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter
        {
            ConnectionString = "Host=localhost;Database=EdFi_Ods;Username=postgres;Password=secret"
        };

        sut.ServerName.ShouldBe("localhost");

        sut.ServerName = "db.internal";

        sut.ServerName.ShouldBe("db.internal");
        sut.ConnectionString.ShouldContain("Host=db.internal");
    }

    [Test]
    public void ServerName_Set_WhenConnectionStringIsNotSet_ShouldThrowInvalidOperationException()
    {
        var sut = new NpgsqlConnectionStringBuilderAdapter();

        var ex = Should.Throw<InvalidOperationException>(() => sut.ServerName = "localhost");

        ex.Message.ShouldBe("Connection string has not been set.");
    }
}
