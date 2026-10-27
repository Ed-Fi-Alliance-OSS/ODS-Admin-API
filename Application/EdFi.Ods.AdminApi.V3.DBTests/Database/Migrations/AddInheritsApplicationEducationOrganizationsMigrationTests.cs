// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.IO;
using EdFi.Ods.AdminApi.DBTestsShared;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.Migrations;

[TestFixture]
public class AddInheritsApplicationEducationOrganizationsMigrationTests : PlatformUsersContextTestBase
{
    private const string ScriptPath = "Migrations/00010.sql";

    protected override string AdminConnectionString => Testing.AdminConnectionString;

    [SetUp]
    public void DropColumn()
    {
        ExecuteNonQuery(
            """
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_ApiClients_InheritsApplicationEducationOrganizations')
                ALTER TABLE dbo.ApiClients DROP CONSTRAINT DF_ApiClients_InheritsApplicationEducationOrganizations;
            IF COL_LENGTH('dbo.ApiClients', 'InheritsApplicationEducationOrganizations') IS NOT NULL
                ALTER TABLE dbo.ApiClients DROP COLUMN InheritsApplicationEducationOrganizations;
            """);
    }

    [TearDown]
    public void RestoreColumn() => RunScript();

    [Test]
    public void Script_WhenColumnMissing_AddsNotNullColumnDefaultingToTrue()
    {
        RunScript();

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand(
            """
            SELECT DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ApiClients' AND COLUMN_NAME = 'InheritsApplicationEducationOrganizations'
            """,
            connection);
        using var reader = command.ExecuteReader();
        reader.Read().ShouldBeTrue();
        reader.GetString(0).ShouldBe("bit");
        reader.GetString(1).ShouldBe("NO");
        reader.Close();

        var apiClientId = InsertApiClient("Default", applicationId: null);
        GetInherits(apiClientId).ShouldBeTrue();
    }

    [Test]
    public void Script_WhenColumnMissing_BackfillsOnlyPartiallyLinkedClientsToFalse()
    {
        var (a, b, c) = SeedApplicationWithClients();

        RunScript();

        GetInherits(a).ShouldBeTrue();
        GetInherits(b).ShouldBeFalse();
        // Zero links: legacy credential created without links, stays inheriting so the next Application edit repairs it.
        GetInherits(c).ShouldBeTrue();
    }

    [Test]
    public void Script_WhenColumnExists_LeavesValuesUntouched()
    {
        var (a, b, _) = SeedApplicationWithClients();
        RunScript();
        ExecuteNonQuery($"UPDATE dbo.ApiClients SET InheritsApplicationEducationOrganizations = 0 WHERE ApiClientId = {a}");
        var bBefore = GetInherits(b);

        RunScript();

        GetInherits(a).ShouldBeFalse();
        GetInherits(b).ShouldBe(bBefore);
    }

    [Test]
    public void Script_RunTwice_DoesNotThrow()
    {
        Should.NotThrow(() =>
        {
            RunScript();
            RunScript();
        });
    }

    private (int A, int B, int C) SeedApplicationWithClients()
    {
        var applicationId = ExecuteScalar(
            """
            INSERT INTO dbo.Applications (ApplicationName, ClaimSetName, OperationalContextUri)
            VALUES ('Migration Test App', 'FakeClaimSet', 'http://test.com');
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """);
        var edOrg1 = InsertApplicationEdOrg(applicationId, 1);
        var edOrg2 = InsertApplicationEdOrg(applicationId, 2);

        var a = InsertApiClient("A", applicationId);
        var b = InsertApiClient("B", applicationId);
        var c = InsertApiClient("C", applicationId);

        Link(a, edOrg1);
        Link(a, edOrg2);
        Link(b, edOrg1);

        return (a, b, c);
    }

    private int InsertApplicationEdOrg(int applicationId, long educationOrganizationId) =>
        ExecuteScalar(
            $"""
            INSERT INTO dbo.ApplicationEducationOrganizations (EducationOrganizationId, Application_ApplicationId)
            VALUES ({educationOrganizationId}, {applicationId});
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """);

    private int InsertApiClient(string name, int? applicationId) =>
        ExecuteScalar(
            $"""
            INSERT INTO dbo.ApiClients ([Key], [Secret], [Name], IsApproved, UseSandbox, SandboxType, SecretIsHashed, Application_ApplicationId)
            VALUES ('{name}Key', 'secret', '{name}', 1, 0, 0, 0, {(applicationId is null ? "NULL" : applicationId.ToString())});
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """);

    private void Link(int apiClientId, int applicationEdOrgId) =>
        ExecuteNonQuery(
            $"""
            INSERT INTO dbo.ApiClientApplicationEducationOrganizations (ApiClient_ApiClientId, ApplicationEducationOrganization_ApplicationEducationOrganizationId)
            VALUES ({apiClientId}, {applicationEdOrgId})
            """);

    private bool GetInherits(int apiClientId) =>
        ExecuteScalar($"SELECT CAST(InheritsApplicationEducationOrganizations AS INT) FROM dbo.ApiClients WHERE ApiClientId = {apiClientId}") == 1;

    private void RunScript() => ExecuteNonQuery(File.ReadAllText(ScriptPath));

    private void ExecuteNonQuery(string sql)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

    private int ExecuteScalar(string sql)
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        return (int) command.ExecuteScalar();
    }
}
