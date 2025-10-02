// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using log4net;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public static class ConnectionStringHelper
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(ConnectionStringHelper));
    public static bool ValidateConnectionString(string databaseEngine, string? connectionString)
    {
        bool result = true;
        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                _ = new SqlConnectionStringBuilder(connectionString);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException ||
                    ex is FormatException ||
                    ex is KeyNotFoundException)
                {
                    result = false;
                    _log.Error(ex);
                }
            }
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                _ = new NpgsqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException ex)
            {
                result = false;
                _log.Error(ex);
            }
        }
        return result;
    }

    public static (string? Host, string? Database) GetHostAndDatabase(string databaseEngine, string? connectionString)
    {
        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return (builder.DataSource, builder.InitialCatalog);
            }
            catch (Exception ex) when (
                ex is ArgumentException or
                FormatException or
                KeyNotFoundException)
            {
                _log.Error(ex);
                return (null, null);
            }
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                return (builder.Host, builder.Database);
            }
            catch (ArgumentException ex)
            {
                _log.Error(ex);
                return (null, null);
            }
        }

        return (null, null);
    }
}
