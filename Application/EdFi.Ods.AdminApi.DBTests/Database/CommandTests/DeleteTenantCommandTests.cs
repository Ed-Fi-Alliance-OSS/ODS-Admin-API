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
public class DeleteTenantCommandTests
{
    private string _testFilePath = string.Empty;

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
        if (_testFilePath is not null && File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Test]
    public void Should_throw_when_appsettings_is_empty()
    {
        _testFilePath = GetTestFilePath(nameof(Should_throw_when_appsettings_is_empty));
        File.WriteAllText(_testFilePath, string.Empty);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new DeleteTenantCommand(provider);

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute("tenant1"));
        ex.Message.ShouldBe("appsettings.json contains invalid JSON.");
    }

    [Test]
    public void Should_throw_when_tenants_section_missing()
    {
        _testFilePath = GetTestFilePath(nameof(Should_throw_when_tenants_section_missing));
        var json = @"{ ""ConnectionStrings"": { ""EdFi_Admin"": ""a"", ""EdFi_Security"": ""b"" } }";
        File.WriteAllText(_testFilePath, json);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new DeleteTenantCommand(provider);

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute("tenant1"));
        ex.Message.ShouldBe("Tenants section missing in appsettings.json.");
    }

    [Test]
    public void Should_throw_when_tenant_does_not_exist()
    {
        _testFilePath = GetTestFilePath(nameof(Should_throw_when_tenant_does_not_exist));
        CopyTestSettings(_testFilePath);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new DeleteTenantCommand(provider);

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute("notarealtenant"));
        ex.Message.ShouldBe("Tenant 'notarealtenant' does not exist.");
    }

    [Test]
    public void Should_throw_when_appsettings_contains_invalid_json()
    {
        _testFilePath = GetTestFilePath(nameof(Should_throw_when_appsettings_contains_invalid_json));
        File.WriteAllText(_testFilePath, "{ invalid json }");

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new DeleteTenantCommand(provider);

        var ex = Should.Throw<InvalidOperationException>(() => command.Execute("tenant1"));
        ex.Message.ShouldBe("appsettings.json contains invalid JSON.");
    }

    [Test]
    public void Should_delete_tenant_when_valid()
    {
        _testFilePath = GetTestFilePath(nameof(Should_delete_tenant_when_valid));
        CopyTestSettings(_testFilePath);

        var provider = new FileSystemAppSettingsFileProvider(_testFilePath);
        var command = new DeleteTenantCommand(provider);

        command.Execute("tenant1");

        var updatedJson = File.ReadAllText(_testFilePath);
        using var doc = JsonDocument.Parse(updatedJson);
        var tenants = doc.RootElement.GetProperty("Tenants");
        tenants.TryGetProperty("tenant1", out _).ShouldBeFalse();
        tenants.TryGetProperty("tenant2", out _).ShouldBeTrue();
    }
}
