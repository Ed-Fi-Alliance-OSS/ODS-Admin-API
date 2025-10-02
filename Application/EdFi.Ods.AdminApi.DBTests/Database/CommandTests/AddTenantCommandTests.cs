// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.IO;
using System.Text.Json;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class AddTenantCommandTests
{
    private string _testFilePath;

    private static string GetTestFilePath(string testName)
    {
        var dir = Path.Combine(Path.GetTempPath(), "AdminApiTests");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{testName}_{Guid.NewGuid()}.json");
    }

    private void CopyTestSettings(string destPath, string sourceFile = "testsappsettings.json")
    {
        var sourcePath = Path.Combine(TestContext.CurrentContext.TestDirectory, sourceFile);
        File.Copy(sourcePath, destPath, true);
    }

    [TearDown]
    public void Cleanup()
    {
        if (!string.IsNullOrEmpty(_testFilePath) && File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Test]
    public void ShouldThrowWhenAppSettingsIsEmpty()
    {
        _testFilePath = GetTestFilePath(nameof(ShouldThrowWhenAppSettingsIsEmpty));
        File.WriteAllText(_testFilePath, string.Empty);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new AddTenantCommand(provider);

        var model = new TestAddTenantModel("newtenant", "sec", "adm");

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute(model));
        ex.Message.ShouldBe("appsettings.json contains invalid JSON.");
    }

    [Test]
    public void ShouldThrowWhenTenantsSectionMissing()
    {
        _testFilePath = GetTestFilePath(nameof(ShouldThrowWhenTenantsSectionMissing));
        var json = @"{ ""ConnectionStrings"": { ""EdFi_Admin"": ""a"", ""EdFi_Security"": ""b"" } }";
        File.WriteAllText(_testFilePath, json);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new AddTenantCommand(provider);

        var model = new TestAddTenantModel("newtenant", "sec", "adm");

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute(model));
        ex.Message.ShouldBe("Tenants section missing in appsettings.json.");
    }

    [Test]
    public void ShouldThrowWhenTenantAlreadyExists()
    {
        _testFilePath = GetTestFilePath(nameof(ShouldThrowWhenTenantAlreadyExists));
        CopyTestSettings(_testFilePath);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new AddTenantCommand(provider);

        var model = new TestAddTenantModel("tenant1", "sec", "adm");

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute(model));
        ex.Message.ShouldBe("Tenant 'tenant1' already exists.");
    }

    [Test]
    public void ShouldThrowWhenAppSettingsContainsInvalidJson()
    {
        _testFilePath = GetTestFilePath(nameof(ShouldThrowWhenAppSettingsContainsInvalidJson));
        File.WriteAllText(_testFilePath, "{ invalid json }");

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new AddTenantCommand(provider);

        var model = new TestAddTenantModel("newtenant", "sec", "adm");

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute(model));
        ex.Message.ShouldBe("appsettings.json contains invalid JSON.");
    }

    [Test]
    public void ShouldAddNewTenantWhenValid()
    {
        _testFilePath = GetTestFilePath(nameof(ShouldAddNewTenantWhenValid));
        CopyTestSettings(_testFilePath);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new AddTenantCommand(provider);

        var model = new TestAddTenantModel("newtenant", "sec-conn", "adm-conn");

        command.Execute(model);

        var updatedJson = File.ReadAllText(_testFilePath);
        using var doc = JsonDocument.Parse(updatedJson);
        var tenants = doc.RootElement.GetProperty("Tenants");
        tenants.TryGetProperty("newtenant", out var newTenant).ShouldBeTrue();
        var connStrings = newTenant.GetProperty("ConnectionStrings");
        connStrings.GetProperty("EdFi_Security").GetString().ShouldBe("sec-conn");
        connStrings.GetProperty("EdFi_Admin").GetString().ShouldBe("adm-conn");
    }

    private class TestAddTenantModel(string tenantName, string sec, string adm) : IAddTenantModel
    {
        public string TenantName { get; } = tenantName;
        public string EdFiSecurityConnectionString { get; } = sec;
        public string EdFiAdminConnectionString { get; } = adm;
    }
}
