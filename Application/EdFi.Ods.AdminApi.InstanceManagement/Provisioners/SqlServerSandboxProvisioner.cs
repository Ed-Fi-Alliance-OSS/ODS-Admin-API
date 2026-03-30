// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using System.Text.RegularExpressions;
using Dapper;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

public class SqlServerSandboxProvisioner : SandboxProvisionerBase
{
    private static readonly Regex _validDatabaseIdentifierPattern = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    private readonly string _sqlServerBakFile;

    public SqlServerSandboxProvisioner(IConfiguration configuration,
        IConfigConnectionStringsProvider connectionStringsProvider, IDatabaseNameBuilder databaseNameBuilder)
        : base(configuration, connectionStringsProvider, databaseNameBuilder)
    {
        _sqlServerBakFile = configuration.GetSection("AppSettings:SqlServerBakFile").Value ?? string.Empty;
    }

    public override async Task RenameSandboxAsync(string oldName, string newName)
    {
        using (var conn = CreateConnection())
        {
            string safeOldName = QuoteDatabaseIdentifier(oldName);
            string safeNewName = QuoteDatabaseIdentifier(newName);

            await ExecuteAsync(
                    conn,
                    $"ALTER DATABASE {safeOldName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            await ExecuteAsync(
                    conn,
                    $"ALTER DATABASE {safeOldName} MODIFY NAME = {safeNewName};",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            await ExecuteAsync(
                    conn,
                    $"ALTER DATABASE {safeNewName} SET MULTI_USER;",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);
        }
    }

    public override async Task DeleteSandboxesAsync(params string[] deletedClientKeys)
    {
        using (var conn = CreateConnection())
        {
            foreach (string database in deletedClientKeys)
            {
                string safeDatabaseName = QuoteDatabaseIdentifier(database);

                string deleteSql =
                    $"""
                     IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName)
                     BEGIN
                         ALTER DATABASE {safeDatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                         DROP DATABASE {safeDatabaseName};
                     END;
                     """;

                await ExecuteAsync(conn, deleteSql, new { DatabaseName = database }, CommandTimeout)
                    .ConfigureAwait(false);
            }
        }
    }

    public override async Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName)
    {
        string safeNewDatabaseName = QuoteDatabaseIdentifier(newDatabaseName);

        using (var conn = CreateConnection())
        {
            string backup = GetBackupFilePath();

            var (dataFilePath, logFilePath) = await GetDatabaseFilePathsAsync(conn, newDatabaseName)
                .ConfigureAwait(false);

            var (logicalDataName, logicalLogName) = await GetLogicalNamesFromBackupAsync(conn, backup)
                .ConfigureAwait(false);

            await ExecuteAsync(
                    conn,
                    $"RESTORE DATABASE {safeNewDatabaseName} FROM DISK = '{backup}' WITH REPLACE, MOVE '{logicalDataName}' TO '{dataFilePath}', MOVE '{logicalLogName}' TO '{logFilePath}';",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            await ExecuteAsync(
                    conn,
                    $"ALTER DATABASE {safeNewDatabaseName} MODIFY FILE (NAME = '{logicalDataName}', NEWNAME = '{newDatabaseName}');",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            await ExecuteAsync(
                    conn,
                    $"ALTER DATABASE {safeNewDatabaseName} MODIFY FILE (NAME = '{logicalLogName}', NEWNAME = '{newDatabaseName}_log');",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);
        }
    }

    public override async Task<SandboxStatus> GetSandboxStatusAsync(string databaseName)
    {
        using (var conn = CreateConnection())
        {
            const string Query = "SELECT name as Name, 0 as Code, 'ONLINE' as Description FROM sys.databases WHERE name = @DatabaseName;";

            var results = await QueryAsync<SandboxStatus>(
                    conn,
                    Query,
                    new { DatabaseName = databaseName },
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            return results.SingleOrDefault() ?? SandboxStatus.ErrorStatus();
        }
    }

    protected override DbConnection CreateConnection() => new SqlConnection(ConnectionString);

    protected virtual string GetBackupFilePath()
    {
        if (string.IsNullOrEmpty(_sqlServerBakFile))
        {
            throw new InvalidOperationException(
                "AppSettings:SqlServerBakFile is not configured. A SQL Server backup file path is required to copy a sandbox database.");
        }

        return _sqlServerBakFile;
    }

    protected virtual async Task<(string DataName, string LogName)> GetLogicalNamesFromBackupAsync(DbConnection conn, string backupFilePath)
    {
        string dataName = string.Empty;
        string logName = string.Empty;

        using (var reader = (DbDataReader)await conn.ExecuteReaderAsync(
                $"RESTORE FILELISTONLY FROM DISK = '{backupFilePath}';",
                commandTimeout: CommandTimeout)
            .ConfigureAwait(false))
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                string logicalName = reader.GetString(0);
                string type = reader.GetString(2);

                if (type.Equals("D", StringComparison.InvariantCultureIgnoreCase))
                {
                    dataName = logicalName;
                }
                else if (type.Equals("L", StringComparison.InvariantCultureIgnoreCase))
                {
                    logName = logicalName;
                }
            }
        }

        if (string.IsNullOrEmpty(dataName) || string.IsNullOrEmpty(logName))
        {
            throw new InvalidOperationException(
                $"Backup file '{backupFilePath}' does not contain expected data (D) and log (L) file entries.");
        }

        return (dataName, logName);
    }

    protected virtual async Task<(string Data, string Log)> GetDatabaseFilePathsAsync(DbConnection conn, string newDatabaseName)
    {
        const string WindowsPlatform = "Windows";
        const string GetHostPlatformSql = "IF OBJECT_ID('sys.dm_os_host_info') IS NOT NULL SELECT host_platform FROM sys.dm_os_host_info ELSE SELECT 'Windows'";

        string? hostPlatform = await ExecuteScalarAsync<string>(conn, GetHostPlatformSql, commandTimeout: CommandTimeout)
            .ConfigureAwait(false);

        bool isWindows = hostPlatform is null || hostPlatform.Equals(WindowsPlatform, StringComparison.InvariantCultureIgnoreCase);

        if (isWindows)
        {
            string? fullPathData = await ExecuteScalarAsync<string>(
                    conn,
                    "SELECT physical_name FROM sys.master_files WHERE type = 0 AND database_id = 1;",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            string? fullPathLog = await ExecuteScalarAsync<string>(
                    conn,
                    "SELECT physical_name FROM sys.master_files WHERE type = 1 AND database_id = 1;",
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            if (fullPathData is null || fullPathLog is null)
            {
                throw new InvalidOperationException(
                    "Unable to determine SQL Server data directory from sys.master_files. Ensure the master database files are accessible.");
            }

            return (
                Data: Path.Combine(Path.GetDirectoryName(fullPathData)!, $"{newDatabaseName}.mdf"),
                Log: Path.Combine(Path.GetDirectoryName(fullPathLog)!, $"{newDatabaseName}_log.ldf")
            );
        }

        return (
            Data: $"/var/opt/mssql/data/{newDatabaseName}.mdf",
            Log: $"/var/opt/mssql/data/{newDatabaseName}.ldf"
        );
    }

    protected virtual Task<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        => connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeout);

    protected virtual Task<IEnumerable<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        => connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);

    protected virtual Task<T?> ExecuteScalarAsync<T>(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        => connection.ExecuteScalarAsync<T>(sql, parameters, commandTimeout: commandTimeout);

    private static string QuoteDatabaseIdentifier(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || !_validDatabaseIdentifierPattern.IsMatch(databaseName))
        {
            throw new ArgumentException("Database name contains invalid characters.", nameof(databaseName));
        }

        return $"[{databaseName}]";
    }
}
