// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using Dapper;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using log4net;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
public class PostgresSandboxProvisioner : SandboxProvisionerBase
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(PostgresSandboxProvisioner));

    public PostgresSandboxProvisioner(IConfiguration configuration,
        IConfigConnectionStringsProvider connectionStringsProvider, IDatabaseNameBuilder databaseNameBuilder)
        : base(configuration, connectionStringsProvider, databaseNameBuilder) { }

    public override async Task RenameSandboxAsync(string oldName, string newName)
    {
        using (var conn = CreateConnection())
        {
            string sql = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='{oldName}';";

            await conn.ExecuteAsync(sql, commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            sql = $"ALTER DATABASE \"{oldName}\" RENAME TO \"{newName}\";";

            await conn.ExecuteAsync(sql, commandTimeout: CommandTimeout)
                .ConfigureAwait(false);
        }
    }

    public override async Task DeleteSandboxesAsync(params string[] databases)
    {
        using (var conn = CreateConnection())
        {
            foreach (string database in databases)
            {
                await conn.ExecuteAsync(
                    $@"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='{database}';");

                await conn.ExecuteAsync(
                        $@"DROP DATABASE IF EXISTS ""{database}"";",
                        commandTimeout: CommandTimeout)
                    .ConfigureAwait(false);
            }
        }
    }

    public override async Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName)
    {
        using (var conn = CreateConnection())
        {
            string sql = @$"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='{originalDatabaseName}';";
            await conn.ExecuteAsync(sql, commandTimeout: CommandTimeout).ConfigureAwait(false);

            sql = @$"CREATE DATABASE ""{newDatabaseName}"" TEMPLATE ""{originalDatabaseName}""";
            await conn.ExecuteAsync(sql, commandTimeout: CommandTimeout).ConfigureAwait(false);
        }
    }

    protected override DbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public override async Task<SandboxStatus> GetSandboxStatusAsync(string database)
    {
        using (var conn = CreateConnection())
        {
            var query = $"SELECT datname as Name, 0 as Code, 'ONLINE' Description FROM pg_database WHERE datname = \'{database}\';";
            var results = await conn.QueryAsync<SandboxStatus>(
                    query,
                    commandTimeout: CommandTimeout)
                .ConfigureAwait(false);

            return results.SingleOrDefault() ?? SandboxStatus.ErrorStatus();
        }
    }
}
