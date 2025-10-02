// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class DeleteTenantCommandTests
{
    private StubFileSystemAppSettingsFileProvider _fileProvider = null!;
    private DeleteTenantCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _fileProvider = new StubFileSystemAppSettingsFileProvider();
        _command = new DeleteTenantCommand(_fileProvider);
    }

    private class StubFileSystemAppSettingsFileProvider : FileSystemAppSettingsFileProvider, IAppSettingsFileProvider
    {
        public StubFileSystemAppSettingsFileProvider() : base("defaultFilePath")
        {
        }

        public string ReadText { get; set; }
        public string WrittenText { get; private set; }

        public new string ReadAllText()
        {
            return ReadText ?? string.Empty;
        }

        public new void WriteAllText(string text)
        {
            WrittenText = text;
        }
    }

    [Test]
    public void Execute_ShouldRemoveTenant_WhenTenantExists()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = @"{
            ""Tenants"": {
                ""tenant1"": {
                    ""ConnectionStrings"": {
                        ""EdFi_Security"": ""sec1"",
                        ""EdFi_Admin"": ""admin1""
                    }
                },
                ""tenant2"": {
                    ""ConnectionStrings"": {
                        ""EdFi_Security"": ""sec2"",
                        ""EdFi_Admin"": ""admin2""
                    }
                }
            }
        }";

        // Act
        _command.Execute("tenant1");

        // Assert
        stubProvider.WrittenText.ShouldNotBeNull();
        var root = JsonNode.Parse(stubProvider.WrittenText!)!;
        var tenants = root["Tenants"]!.AsObject();
        tenants.ContainsKey("tenant1").ShouldBeFalse();
        tenants.ContainsKey("tenant2").ShouldBeTrue();
    }

    [Test]
    public void Execute_ShouldThrow_WhenTenantDoesNotExist()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = @"{
            ""Tenants"": {
                ""tenant2"": {
                    ""ConnectionStrings"": {
                        ""EdFi_Security"": ""sec2"",
                        ""EdFi_Admin"": ""admin2""
                    }
                }
            }
        }";

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _command.Execute("tenant1"));
        ex.Message.ShouldContain("does not exist");
    }

    [Test]
    public void Execute_ShouldThrow_WhenTenantsSectionMissing()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = @"{ ""SomeOtherSection"": {} }";

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _command.Execute("tenant1"));
        ex.Message.ShouldContain("Tenants section missing");
    }

    [Test]
    public void Execute_ShouldThrow_WhenAppSettingsIsEmptyOrInvalid()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = string.Empty;

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _command.Execute("tenant1"));
        ex.Message.ShouldContain("appsettings.json contains invalid JSON.");
    }
}
