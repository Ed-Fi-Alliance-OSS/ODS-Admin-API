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

    public static string ConnectionStringRename(string databaseEngine, string? connectionString, string newDbName)
    {
        if (string.IsNullOrEmpty(connectionString))
            return string.Empty;

        if (databaseEngine.Equals(DatabaseEngineEnum.SqlServer, StringComparison.InvariantCultureIgnoreCase))
        {
            var connectrionStringBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = newDbName
            };
            return connectrionStringBuilder.ToString();
        }
        else if (databaseEngine.Equals(DatabaseEngineEnum.PostgreSql, StringComparison.InvariantCultureIgnoreCase))
        {
            var connectrionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = newDbName
            };
            return connectrionStringBuilder.ToString();
        }
        else
            throw new NotSupportedException();
    }
}
