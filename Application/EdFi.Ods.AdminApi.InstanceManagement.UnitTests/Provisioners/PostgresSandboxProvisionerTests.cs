// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace EdFi.Ods.AdminApi.InstanceManagement.UnitTests.Provisioners;

[TestFixture]
public class PostgresSandboxProvisionerTests
{
    [Test]
    public async Task RenameSandboxAsync_ShouldTerminateConnectionsThenRenameDatabase()
    {
        var sut = CreateSut();

        await sut.RenameSandboxAsync("OldDb", "NewDb");

        sut.Executions.Count.ShouldBe(2);

        var terminate = sut.Executions[0];
        terminate.Sql.ShouldBe("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;");
        GetDatabaseNameParam(terminate.Parameters).ShouldBe("OldDb");
        terminate.CommandTimeout.ShouldBe(30);

        var rename = sut.Executions[1];
        rename.Sql.ShouldBe("ALTER DATABASE \"OldDb\" RENAME TO \"NewDb\";");
        rename.Parameters.ShouldBeNull();
        rename.CommandTimeout.ShouldBe(30);
    }

    [Test]
    public async Task DeleteSandboxesAsync_ShouldTerminateAndDropEachDatabase()
    {
        var sut = CreateSut();

        await sut.DeleteSandboxesAsync("db1", "db2");

        sut.Executions.Count.ShouldBe(4);

        sut.Executions[0].Sql.ShouldBe("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;");
        GetDatabaseNameParam(sut.Executions[0].Parameters).ShouldBe("db1");
        sut.Executions[1].Sql.ShouldBe("DROP DATABASE IF EXISTS \"db1\";");

        sut.Executions[2].Sql.ShouldBe("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;");
        GetDatabaseNameParam(sut.Executions[2].Parameters).ShouldBe("db2");
        sut.Executions[3].Sql.ShouldBe("DROP DATABASE IF EXISTS \"db2\";");

        sut.Executions.All(x => x.CommandTimeout == 30).ShouldBeTrue();
    }

    [Test]
    public async Task CopySandboxAsync_ShouldTerminateSourceConnectionsThenCreateDatabaseFromTemplate()
    {
        var sut = CreateSut();

        await sut.CopySandboxAsync("TemplateDb", "TenantDb");

        sut.Executions.Count.ShouldBe(2);

        var terminate = sut.Executions[0];
        terminate.Sql.ShouldBe("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;");
        GetDatabaseNameParam(terminate.Parameters).ShouldBe("TemplateDb");

        var create = sut.Executions[1];
        create.Sql.ShouldBe("CREATE DATABASE \"TenantDb\" TEMPLATE \"TemplateDb\";");
        create.Parameters.ShouldBeNull();
    }

    [Test]
    public async Task GetSandboxStatusAsync_WhenResultExists_ShouldReturnFirstResult()
    {
        var expected = new SandboxStatus("TenantDb", "ONLINE")
        {
            Code = 0
        };
        var sut = CreateSut([expected]);

        var result = await sut.GetSandboxStatusAsync("TenantDb");

        result.ShouldBeSameAs(expected);
        sut.Queries.Count.ShouldBe(1);

        var query = sut.Queries[0];
        query.Sql.ShouldBe("SELECT datname as Name, 0 as Code, 'ONLINE' Description FROM pg_database WHERE datname = @DatabaseName;");
        GetDatabaseNameParam(query.Parameters).ShouldBe("TenantDb");
        query.CommandTimeout.ShouldBe(30);
    }

    [Test]
    public async Task GetSandboxStatusAsync_WhenNoResult_ShouldReturnErrorStatus()
    {
        var sut = CreateSut([]);

        var result = await sut.GetSandboxStatusAsync("MissingDb");

        result.Code.ShouldBe(byte.MaxValue);
        result.Description.ShouldBe("ERROR");
    }

    [TestCase("bad-name")]
    [TestCase("db name")]
    [TestCase("db;")]
    [TestCase("")]
    public void RenameSandboxAsync_WithInvalidIdentifier_ShouldThrowArgumentException(string invalidName)
    {
        var sut = CreateSut();

        var ex = Should.ThrowAsync<ArgumentException>(() => sut.RenameSandboxAsync(invalidName, "ValidName"));

        ex.Result.ParamName.ShouldBe("databaseName");
    }

    [Test]
    public void CopySandboxAsync_WithInvalidTargetIdentifier_ShouldThrowArgumentException()
    {
        var sut = CreateSut();

        var ex = Should.ThrowAsync<ArgumentException>(() => sut.CopySandboxAsync("ValidName", "bad-name"));

        ex.Result.ParamName.ShouldBe("databaseName");
    }

    private static string? GetDatabaseNameParam(object? parameters)
    {
        if (parameters == null)
        {
            return null;
        }

        return parameters.GetType().GetProperty("DatabaseName")?.GetValue(parameters)?.ToString();
    }

    private static TestablePostgresSandboxProvisioner CreateSut(IEnumerable<SandboxStatus>? queryResults = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SandboxAdminSQLCommandTimeout"] = "30"
            })
            .Build();

        var connectionStringsProvider = A.Fake<IConfigConnectionStringsProvider>();
        var databaseNameBuilder = A.Fake<IDatabaseNameBuilder>();

        A.CallTo(() => connectionStringsProvider.GetConnectionString("EdFi_Master"))
            .Returns("Host=localhost;Database=postgres");

        return new TestablePostgresSandboxProvisioner(configuration, connectionStringsProvider, databaseNameBuilder)
        {
            QueryResults = queryResults?.ToList() ?? []
        };
    }

    private sealed class TestablePostgresSandboxProvisioner : PostgresSandboxProvisioner
    {
        public TestablePostgresSandboxProvisioner(
            IConfiguration configuration,
            IConfigConnectionStringsProvider connectionStringsProvider,
            IDatabaseNameBuilder databaseNameBuilder)
            : base(configuration, connectionStringsProvider, databaseNameBuilder)
        {
        }

        public List<ExecutionRecord> Executions { get; } = [];

        public List<QueryRecord> Queries { get; } = [];

        public List<SandboxStatus> QueryResults { get; set; } = [];

        protected override DbConnection CreateConnection() => new NoopDbConnection();

        protected override Task<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        {
            Executions.Add(new ExecutionRecord(sql, parameters, commandTimeout));
            return Task.FromResult(1);
        }

        protected override Task<IEnumerable<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        {
            Queries.Add(new QueryRecord(sql, parameters, commandTimeout));

            var results = QueryResults.Cast<T>();
            return Task.FromResult(results);
        }
    }

    private sealed record ExecutionRecord(string Sql, object? Parameters, int? CommandTimeout);

    private sealed record QueryRecord(string Sql, object? Parameters, int? CommandTimeout);

    private sealed class NoopDbConnection : DbConnection
    {
        private string _connectionString = string.Empty;

        [AllowNull]
        public override string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value ?? string.Empty;
        }

        public override string Database => string.Empty;

        public override string DataSource => string.Empty;

        public override string ServerVersion => string.Empty;

        public override ConnectionState State => ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName)
            => throw new NotSupportedException();

        public override void Open()
            => throw new NotSupportedException();

        public override void Close()
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand()
            => throw new NotSupportedException();
    }
}
