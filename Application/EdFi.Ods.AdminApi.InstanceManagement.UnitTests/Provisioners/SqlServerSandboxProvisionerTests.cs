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
public class SqlServerSandboxProvisionerTests
{
    [Test]
    public async Task RenameSandboxAsync_ShouldTerminateConnectionsThenRenameThenRestoreMultiUser()
    {
        var sut = CreateSut();

        await sut.RenameSandboxAsync("OldDb", "NewDb");

        sut.Executions.Count.ShouldBe(3);

        sut.Executions[0].Sql.ShouldBe("ALTER DATABASE [OldDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
        sut.Executions[0].Parameters.ShouldBeNull();
        sut.Executions[0].CommandTimeout.ShouldBe(30);

        sut.Executions[1].Sql.ShouldBe("ALTER DATABASE [OldDb] MODIFY NAME = [NewDb];");
        sut.Executions[1].Parameters.ShouldBeNull();
        sut.Executions[1].CommandTimeout.ShouldBe(30);

        sut.Executions[2].Sql.ShouldBe("ALTER DATABASE [NewDb] SET MULTI_USER;");
        sut.Executions[2].Parameters.ShouldBeNull();
        sut.Executions[2].CommandTimeout.ShouldBe(30);
    }

    [Test]
    public async Task RenameSandboxAsync_WhenRenameFails_ShouldRestoreMultiUserOnOldName()
    {
        var sut = CreateSut();
        sut.ThrowOnSql = "MODIFY NAME";

        await Should.ThrowAsync<InvalidOperationException>(() => sut.RenameSandboxAsync("OldDb", "NewDb"));

        sut.Executions.Count.ShouldBe(2);
        sut.Executions[0].Sql.ShouldContain("SET SINGLE_USER");
        sut.Executions[1].Sql.ShouldBe("ALTER DATABASE [OldDb] SET MULTI_USER;");
    }

    [Test]
    public async Task DeleteSandboxesAsync_ShouldSetSingleUserAndDropEachDatabase()
    {
        var sut = CreateSut();

        await sut.DeleteSandboxesAsync("db1", "db2");

        sut.Executions.Count.ShouldBe(2);

        var first = sut.Executions[0];
        first.Sql.ShouldContain("[db1]");
        first.Sql.ShouldContain("SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        first.Sql.ShouldContain("DROP DATABASE [db1]");
        GetDatabaseNameParam(first.Parameters).ShouldBe("db1");
        first.CommandTimeout.ShouldBe(30);

        var second = sut.Executions[1];
        second.Sql.ShouldContain("[db2]");
        second.Sql.ShouldContain("SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        second.Sql.ShouldContain("DROP DATABASE [db2]");
        GetDatabaseNameParam(second.Parameters).ShouldBe("db2");
    }

    [Test]
    public async Task CopySandboxAsync_WithMinimalDatabase_ShouldRestoreFromMinimalBakFile()
    {
        var sut = CreateSut();
        sut.LogicalNames = ("EdFi_Ods", "EdFi_Ods_log");
        sut.FilePaths = (@"C:\Data\TenantDb.mdf", @"C:\Data\TenantDb_log.ldf");

        await sut.CopySandboxAsync("EdFi_Ods_Minimal_Template", "TenantDb");

        sut.Executions.Count.ShouldBe(3);

        var restore = sut.Executions[0];
        restore.Sql.ShouldBe(@"RESTORE DATABASE [TenantDb] FROM DISK = 'C:\Backups\minimal.bak' WITH REPLACE, MOVE 'EdFi_Ods' TO 'C:\Data\TenantDb.mdf', MOVE 'EdFi_Ods_log' TO 'C:\Data\TenantDb_log.ldf';");
        restore.Parameters.ShouldBeNull();

        var renameData = sut.Executions[1];
        renameData.Sql.ShouldBe("ALTER DATABASE [TenantDb] MODIFY FILE (NAME = 'EdFi_Ods', NEWNAME = 'TenantDb');");

        var renameLog = sut.Executions[2];
        renameLog.Sql.ShouldBe("ALTER DATABASE [TenantDb] MODIFY FILE (NAME = 'EdFi_Ods_log', NEWNAME = 'TenantDb_log');");

        sut.Executions.All(x => x.CommandTimeout == 30).ShouldBeTrue();
    }

    [Test]
    public async Task CopySandboxAsync_WithSampleDatabase_ShouldRestoreFromSampleBakFile()
    {
        var sut = CreateSut();
        sut.LogicalNames = ("EdFi_Ods", "EdFi_Ods_log");
        sut.FilePaths = (@"C:\Data\TenantDb.mdf", @"C:\Data\TenantDb_log.ldf");

        await sut.CopySandboxAsync("EdFi_Ods_Sample_Template", "TenantDb");

        var restore = sut.Executions[0];
        restore.Sql.ShouldContain(@"C:\Backups\sample.bak");
        restore.Sql.ShouldNotContain(@"C:\Backups\minimal.bak");
    }

    [Test]
    public async Task GetSandboxStatusAsync_WhenResultExists_ShouldReturnFirstResult()
    {
        var expected = new SandboxStatus("TenantDb", "ONLINE") { Code = 0 };
        var sut = CreateSut([expected]);

        var result = await sut.GetSandboxStatusAsync("TenantDb");

        result.ShouldBeSameAs(expected);
        sut.Queries.Count.ShouldBe(1);

        var query = sut.Queries[0];
        query.Sql.ShouldBe("SELECT name as Name, state as Code, state_desc as Description FROM sys.databases WHERE name = @DatabaseName;");
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
    public void RenameSandboxAsync_WithInvalidOldName_ShouldThrowArgumentException(string invalidName)
    {
        var sut = CreateSut();

        var ex = Should.ThrowAsync<ArgumentException>(() => sut.RenameSandboxAsync(invalidName, "ValidName"));

        ex.Result.ParamName.ShouldBe("databaseName");
    }

    [TestCase("bad-name")]
    [TestCase("db name")]
    [TestCase("db;")]
    [TestCase("")]
    public void RenameSandboxAsync_WithInvalidNewName_ShouldThrowArgumentException(string invalidName)
    {
        var sut = CreateSut();

        var ex = Should.ThrowAsync<ArgumentException>(() => sut.RenameSandboxAsync("ValidName", invalidName));

        ex.Result.ParamName.ShouldBe("databaseName");
    }

    [TestCase("bad-name")]
    [TestCase("db name")]
    [TestCase("db;")]
    [TestCase("")]
    public void DeleteSandboxesAsync_WithInvalidIdentifier_ShouldThrowArgumentException(string invalidName)
    {
        var sut = CreateSut();

        var ex = Should.ThrowAsync<ArgumentException>(() => sut.DeleteSandboxesAsync(invalidName));

        ex.Result.ParamName.ShouldBe("databaseName");
    }

    [Test]
    public void CopySandboxAsync_WithInvalidTargetIdentifier_ShouldThrowArgumentException()
    {
        var sut = CreateSut();

        var ex = Should.ThrowAsync<ArgumentException>(() => sut.CopySandboxAsync("EdFi_Ods_Minimal_Template", "bad-name"));

        ex.Result.ParamName.ShouldBe("databaseName");
    }

    private static string? GetDatabaseNameParam(object? parameters)
    {
        if (parameters is null)
        {
            return null;
        }

        return parameters.GetType().GetProperty("DatabaseName")?.GetValue(parameters)?.ToString();
    }

    private static TestableSqlServerSandboxProvisioner CreateSut(IEnumerable<SandboxStatus>? queryResults = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SandboxAdminSQLCommandTimeout"] = "30",
                ["AppSettings:SqlServerMinimalBakFile"] = @"C:\Backups\minimal.bak",
                ["AppSettings:SqlServerSampleBakFile"] = @"C:\Backups\sample.bak"
            })
            .Build();

        var connectionStringsProvider = A.Fake<IConfigConnectionStringsProvider>();
        var databaseNameBuilder = A.Fake<IDatabaseNameBuilder>();

        A.CallTo(() => connectionStringsProvider.GetConnectionString("EdFi_Master"))
            .Returns("Data Source=localhost;Initial Catalog=master;Integrated Security=True;");

        A.CallTo(() => databaseNameBuilder.MinimalDatabase).Returns("EdFi_Ods_Minimal_Template");
        A.CallTo(() => databaseNameBuilder.SampleDatabase).Returns("EdFi_Ods_Sample_Template");

        return new TestableSqlServerSandboxProvisioner(configuration, connectionStringsProvider, databaseNameBuilder)
        {
            QueryResults = queryResults?.ToList() ?? []
        };
    }

    private sealed class TestableSqlServerSandboxProvisioner : SqlServerSandboxProvisioner
    {
        public TestableSqlServerSandboxProvisioner(
            IConfiguration configuration,
            IConfigConnectionStringsProvider connectionStringsProvider,
            IDatabaseNameBuilder databaseNameBuilder)
            : base(configuration, connectionStringsProvider, databaseNameBuilder)
        {
        }

        public List<ExecutionRecord> Executions { get; } = [];

        public List<QueryRecord> Queries { get; } = [];

        public List<SandboxStatus> QueryResults { get; set; } = [];

        public (string DataName, string LogName) LogicalNames { get; set; } = ("EdFi_Ods", "EdFi_Ods_log");

        public (string Data, string Log) FilePaths { get; set; } = (@"C:\Data\NewDb.mdf", @"C:\Data\NewDb_log.ldf");

        public string? ThrowOnSql { get; set; }

        protected override DbConnection CreateConnection() => new NoopDbConnection();

        protected override Task<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        {
            if (ThrowOnSql is not null && sql.Contains(ThrowOnSql))
            {
                throw new InvalidOperationException($"Simulated SQL failure on: {ThrowOnSql}");
            }

            Executions.Add(new ExecutionRecord(sql, parameters, commandTimeout));

            return Task.FromResult(1);
        }

        protected override Task<IEnumerable<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        {
            Queries.Add(new QueryRecord(sql, parameters, commandTimeout));
            var results = QueryResults.Cast<T>();

            return Task.FromResult(results);
        }

        protected override Task<(string DataName, string LogName)> GetLogicalNamesFromBackupAsync(DbConnection conn, string backupFilePath)
            => Task.FromResult(LogicalNames);

        protected override Task<(string Data, string Log)> GetDatabaseFilePathsAsync(DbConnection conn, string newDatabaseName)
            => Task.FromResult(FilePaths);
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
