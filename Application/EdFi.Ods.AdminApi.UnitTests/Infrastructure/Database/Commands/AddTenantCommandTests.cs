// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddTenantCommandTests
{
    private StubFileSystemAppSettingsFileProvider _fileProvider = null!;
    private AddTenantCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _fileProvider = new StubFileSystemAppSettingsFileProvider();
        _command = new AddTenantCommand(_fileProvider);
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

    // Update tests to use the stub
    [Test]
    public void Execute_ShouldAddTenant_WhenTenantDoesNotExist()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = @"{
            ""Tenants"": {
                ""existingTenant"": {
                    ""ConnectionStrings"": {
                        ""EdFi_Security"": ""sec"",
                        ""EdFi_Admin"": ""admin""
                    }
                }
            }
        }";

        var model = A.Fake<IAddTenantModel>();
        A.CallTo(() => model.TenantName).Returns("newTenant");
        A.CallTo(() => model.EdFiSecurityConnectionString).Returns("sec2");
        A.CallTo(() => model.EdFiAdminConnectionString).Returns("admin2");

        // Act
        _command.Execute(model);

        // Assert
        stubProvider.WrittenText.ShouldNotBeNull();
        var root = JsonNode.Parse(stubProvider.WrittenText!)!;
        var tenants = root["Tenants"]!.AsObject();
        tenants.ContainsKey("newTenant").ShouldBeTrue();
        tenants["newTenant"]!["ConnectionStrings"]!["EdFi_Security"]!.ToString().ShouldBe("sec2");
        tenants["newTenant"]!["ConnectionStrings"]!["EdFi_Admin"]!.ToString().ShouldBe("admin2");
    }

    [Test]
    public void Execute_ShouldThrow_WhenTenantAlreadyExists()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = @"{
            ""Tenants"": {
                ""existingTenant"": {
                    ""ConnectionStrings"": {
                        ""EdFi_Security"": ""sec"",
                        ""EdFi_Admin"": ""admin""
                    }
                }
            }
        }";

        var model = A.Fake<IAddTenantModel>();
        A.CallTo(() => model.TenantName).Returns("existingTenant");
        A.CallTo(() => model.EdFiSecurityConnectionString).Returns("sec");
        A.CallTo(() => model.EdFiAdminConnectionString).Returns("admin");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _command.Execute(model));
        ex.Message.ShouldContain("already exists");
    }

    [Test]
    public void Execute_ShouldThrow_WhenTenantsSectionMissing()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = @"{ ""SomeOtherSection"": {} }";

        var model = A.Fake<IAddTenantModel>();
        A.CallTo(() => model.TenantName).Returns("tenantX");
        A.CallTo(() => model.EdFiSecurityConnectionString).Returns("secX");
        A.CallTo(() => model.EdFiAdminConnectionString).Returns("adminX");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _command.Execute(model));
        ex.Message.ShouldContain("Tenants section missing");
    }

    [Test]
    public void Execute_ShouldThrow_WhenAppSettingsIsEmptyOrInvalid()
    {
        // Arrange
        var stubProvider = _fileProvider;
        stubProvider.ReadText = string.Empty;

        var model = A.Fake<IAddTenantModel>();
        A.CallTo(() => model.TenantName).Returns("tenantY");
        A.CallTo(() => model.EdFiSecurityConnectionString).Returns("secY");
        A.CallTo(() => model.EdFiAdminConnectionString).Returns("adminY");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _command.Execute(model));
        ex.Message.ShouldContain("appsettings.json contains invalid JSON.");
    }
}
