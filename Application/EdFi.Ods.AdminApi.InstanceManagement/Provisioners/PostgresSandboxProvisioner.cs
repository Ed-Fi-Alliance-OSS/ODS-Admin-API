// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using System.Text.RegularExpressions;
using Dapper;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

public class PostgresSandboxProvisioner : SandboxProvisionerBase
{
    private static readonly Regex _validDatabaseIdentifierPattern = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public PostgresSandboxProvisioner(IConfiguration configuration,
        IConfigConnectionStringsProvider connectionStringsProvider, IDatabaseNameBuilder databaseNameBuilder)
        : base(configuration, connectionStringsProvider, databaseNameBuilder) { }

    public override async Task RenameSandboxAsync(string oldName, string newName)
    {
        using (var conn = CreateConnection())
        {
            string safeOldName = QuoteDatabaseIdentifier(oldName);
            string safeNewName = QuoteDatabaseIdentifier(newName);
            const string TerminateConnectionsSql = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;";

            await ExecuteAsync(conn, TerminateConnectionsSql, new { DatabaseName = oldName }, CommandTimeout)
                .ConfigureAwait(false);

            string renameSql = $"ALTER DATABASE {safeOldName} RENAME TO {safeNewName};";

            await ExecuteAsync(conn, renameSql, commandTimeout: CommandTimeout)
                .ConfigureAwait(false);
        }
    }

    public override async Task DeleteSandboxesAsync(params string[] deletedClientKeys)
    {
        using (var conn = CreateConnection())
        {
            const string TerminateConnectionsSql = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;";

            foreach (string database in deletedClientKeys)
            {
                string safeDatabaseName = QuoteDatabaseIdentifier(database);

                await ExecuteAsync(
                    conn,
                    TerminateConnectionsSql,
                    new { DatabaseName = database },
                    commandTimeout: CommandTimeout)
                    .ConfigureAwait(false);

                await ExecuteAsync(
                    conn,
                        $"DROP DATABASE IF EXISTS {safeDatabaseName};",
                        commandTimeout: CommandTimeout)
                    .ConfigureAwait(false);
            }
        }
    }

    public override async Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName)
    {
        using (var conn = CreateConnection())
        {
            string safeOriginalDatabaseName = QuoteDatabaseIdentifier(originalDatabaseName);
            string safeNewDatabaseName = QuoteDatabaseIdentifier(newDatabaseName);
            const string TerminateConnectionsSql = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @DatabaseName;";

            await ExecuteAsync(
                    conn,
                    TerminateConnectionsSql,
                    new { DatabaseName = originalDatabaseName },
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            string createSql = $"CREATE DATABASE {safeNewDatabaseName} TEMPLATE {safeOriginalDatabaseName};";
            await ExecuteAsync(conn, createSql, commandTimeout: CommandTimeout).ConfigureAwait(false);
        }
    }

    protected override DbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public override async Task<SandboxStatus> GetSandboxStatusAsync(string clientKey)
    {
        using (var conn = CreateConnection())
        {
            const string Query = "SELECT datname as Name, 0 as Code, 'ONLINE' Description FROM pg_database WHERE datname = @DatabaseName;";
            var results = await QueryAsync<SandboxStatus>(
                    conn,
                    Query,
                    new { DatabaseName = clientKey },
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            return results.SingleOrDefault() ?? SandboxStatus.ErrorStatus();
        }
    }

    protected virtual Task<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        => connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeout);

    protected virtual Task<IEnumerable<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, int? commandTimeout = null)
        => connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);

    private static string QuoteDatabaseIdentifier(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || !_validDatabaseIdentifierPattern.IsMatch(databaseName))
        {
            throw new ArgumentException("Database name contains invalid characters.", nameof(databaseName));
        }

        return $"\"{databaseName}\"";
    }
}
