// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using System.Text.RegularExpressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace EdFi.Ods.AdminApi.InstanceManagement.UnitTests.Provisioners;

[TestFixture]
public class SandboxProvisionerBaseTests
{
    [Test]
    public void Constructor_WhenTimeoutConfigured_ShouldUseConfiguredTimeoutAndMasterConnectionString()
    {
        var configuration = BuildConfiguration("45");
        var connectionStringsProvider = A.Fake<IConfigConnectionStringsProvider>();
        var databaseNameBuilder = A.Fake<IDatabaseNameBuilder>();

        A.CallTo(() => connectionStringsProvider.GetConnectionString("EdFi_Master"))
            .Returns("Host=localhost;Database=postgres");

        var sut = new TestSandboxProvisioner(configuration, connectionStringsProvider, databaseNameBuilder);

        sut.ExposedCommandTimeout.ShouldBe(45);
        sut.ExposedConnectionString.ShouldBe("Host=localhost;Database=postgres");
        A.CallTo(() => connectionStringsProvider.GetConnectionString("EdFi_Master"))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Constructor_WhenTimeoutIsMissing_ShouldDefaultToThirtySeconds()
    {
        var configuration = BuildConfiguration(timeoutValue: null);
        var connectionStringsProvider = A.Fake<IConfigConnectionStringsProvider>();
        var databaseNameBuilder = A.Fake<IDatabaseNameBuilder>();

        A.CallTo(() => connectionStringsProvider.GetConnectionString("EdFi_Master"))
            .Returns("Host=localhost;Database=postgres");

        var sut = new TestSandboxProvisioner(configuration, connectionStringsProvider, databaseNameBuilder);

        sut.ExposedCommandTimeout.ShouldBe(30);
    }

    [Test]
    public async Task AddSandboxAsync_WithMinimalType_ShouldDeleteThenCopyFromMinimalTemplate()
    {
        var sut = CreateSut();

        await sut.AddSandboxAsync("TenantDb", SandboxType.Minimal);

        sut.CallOrder.ShouldBe(["Delete", "Copy"]);
        sut.DeleteCalls.Count.ShouldBe(1);
        sut.DeleteCalls[0].ShouldBe(["TenantDb"]);
        sut.CopyCalls.Count.ShouldBe(1);
        sut.CopyCalls[0].ShouldBe(("Minimal_Template_Db", "TenantDb"));
    }

    [Test]
    public async Task AddSandboxAsync_WithSampleType_ShouldDeleteThenCopyFromSampleTemplate()
    {
        var sut = CreateSut();

        await sut.AddSandboxAsync("TenantDb", SandboxType.Sample);

        sut.CallOrder.ShouldBe(["Delete", "Copy"]);
        sut.DeleteCalls.Count.ShouldBe(1);
        sut.DeleteCalls[0].ShouldBe(["TenantDb"]);
        sut.CopyCalls.Count.ShouldBe(1);
        sut.CopyCalls[0].ShouldBe(("Sample_Template_Db", "TenantDb"));
    }

    [Test]
    public async Task AddSandboxAsync_WithUnknownType_ShouldThrowArgumentOutOfRangeException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => sut.AddSandboxAsync("TenantDb", (SandboxType)999));

        ex.ParamName.ShouldBe("sandboxType");
        sut.DeleteCalls.Count.ShouldBe(1);
        sut.CopyCalls.Count.ShouldBe(0);
    }

    [Test]
    public async Task ResetDemoSandboxAsync_ShouldCopyThenDeleteDemoThenRenameTempToDemo()
    {
        var sut = CreateSut();

        await sut.ResetDemoSandboxAsync();

        sut.CallOrder.ShouldBe(["Copy", "Delete", "Rename"]);
        sut.CopyCalls.Count.ShouldBe(1);
        sut.DeleteCalls.Count.ShouldBe(1);
        sut.RenameCalls.Count.ShouldBe(1);

        var copyCall = sut.CopyCalls[0];
        var deleteCall = sut.DeleteCalls[0];
        var renameCall = sut.RenameCalls[0];

        copyCall.OriginalDatabase.ShouldBe("Sample_Template_Db");
        deleteCall.ShouldBe(["EdFi_Ods"]);
        renameCall.NewName.ShouldBe("EdFi_Ods");
        renameCall.OldName.ShouldBe(copyCall.NewDatabase);
        Regex.IsMatch(copyCall.NewDatabase, "^[a-f0-9]{32}$").ShouldBeTrue();
    }

    [Test]
    public void AddSandbox_ShouldDelegateToAsyncImplementation()
    {
        var sut = CreateSut();

        sut.AddSandbox("TenantDb", SandboxType.Minimal);

        sut.DeleteCalls.Count.ShouldBe(1);
        sut.CopyCalls.Count.ShouldBe(1);
    }

    [Test]
    public void DeleteSandboxes_ShouldDelegateToAsyncImplementation()
    {
        var sut = CreateSut();

        sut.DeleteSandboxes("db1", "db2");

        sut.DeleteCalls.Count.ShouldBe(1);
        sut.DeleteCalls[0].ShouldBe(["db1", "db2"]);
    }

    [Test]
    public void RenameSandbox_ShouldDelegateToAsyncImplementation()
    {
        var sut = CreateSut();

        sut.RenameSandbox("old_db", "new_db");

        sut.RenameCalls.Count.ShouldBe(1);
        sut.RenameCalls[0].ShouldBe(("old_db", "new_db"));
    }

    [Test]
    public void GetSandboxStatus_ShouldDelegateToAsyncImplementation()
    {
        var expectedStatus = new SandboxStatus("TenantDb", "ONLINE")
        {
            Code = 7
        };
        var sut = CreateSut(expectedStatus);

        var status = sut.GetSandboxStatus("TenantDb");

        status.ShouldBeSameAs(expectedStatus);
        sut.GetStatusCalls.Count.ShouldBe(1);
        sut.GetStatusCalls[0].ShouldBe("TenantDb");
    }

    private static TestSandboxProvisioner CreateSut(SandboxStatus? statusToReturn = null)
    {
        var configuration = BuildConfiguration("30");
        var connectionStringsProvider = A.Fake<IConfigConnectionStringsProvider>();
        var databaseNameBuilder = A.Fake<IDatabaseNameBuilder>();

        A.CallTo(() => connectionStringsProvider.GetConnectionString("EdFi_Master"))
            .Returns("Host=localhost;Database=postgres");

        A.CallTo(() => databaseNameBuilder.MinimalDatabase).Returns("Minimal_Template_Db");
        A.CallTo(() => databaseNameBuilder.SampleDatabase).Returns("Sample_Template_Db");
        A.CallTo(() => databaseNameBuilder.DemoSandboxDatabase).Returns("EdFi_Ods");

        return new TestSandboxProvisioner(configuration, connectionStringsProvider, databaseNameBuilder)
        {
            StatusToReturn = statusToReturn ?? new SandboxStatus("default", "ONLINE")
        };
    }

    private static IConfiguration BuildConfiguration(string? timeoutValue)
    {
        var configurationData = new Dictionary<string, string?>();

        if (timeoutValue is not null)
        {
            configurationData["SandboxAdminSQLCommandTimeout"] = timeoutValue;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
    }

    private sealed class TestSandboxProvisioner : SandboxProvisionerBase
    {
        public TestSandboxProvisioner(
            IConfiguration configuration,
            IConfigConnectionStringsProvider connectionStringsProvider,
            IDatabaseNameBuilder databaseNameBuilder)
            : base(configuration, connectionStringsProvider, databaseNameBuilder)
        {
        }

        public int ExposedCommandTimeout => CommandTimeout;

        public string ExposedConnectionString => ConnectionString;

        public List<string[]> DeleteCalls { get; } = [];

        public List<(string OldName, string NewName)> RenameCalls { get; } = [];

        public List<(string OriginalDatabase, string NewDatabase)> CopyCalls { get; } = [];

        public List<string> GetStatusCalls { get; } = [];

        public List<string> CallOrder { get; } = [];

        public SandboxStatus StatusToReturn { get; set; } = new SandboxStatus("default", "ONLINE");

        public override Task DeleteSandboxesAsync(params string[] deletedClientKeys)
        {
            CallOrder.Add("Delete");
            DeleteCalls.Add(deletedClientKeys);
            return Task.CompletedTask;
        }

        public override Task<SandboxStatus> GetSandboxStatusAsync(string clientKey)
        {
            CallOrder.Add("GetStatus");
            GetStatusCalls.Add(clientKey);
            return Task.FromResult(StatusToReturn);
        }

        public override Task RenameSandboxAsync(string oldName, string newName)
        {
            CallOrder.Add("Rename");
            RenameCalls.Add((oldName, newName));
            return Task.CompletedTask;
        }

        public override Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName)
        {
            CallOrder.Add("Copy");
            CopyCalls.Add((originalDatabaseName, newDatabaseName));
            return Task.CompletedTask;
        }

        protected override DbConnection CreateConnection()
            => throw new NotSupportedException("CreateConnection should not be called by these tests.");
    }
}
