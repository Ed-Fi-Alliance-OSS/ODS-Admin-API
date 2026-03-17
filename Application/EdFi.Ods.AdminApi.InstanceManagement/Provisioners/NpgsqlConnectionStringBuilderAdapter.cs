// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Npgsql;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

/// <summary>
/// Implements a PostgreSQL-specific adapter for the <see cref="DbConnectionStringBuilder" />
/// that provides the database name through the <see cref="Npgsql.NpgsqlConnectionStringBuilder.Database" />
/// property.
/// </summary>
public class NpgsqlConnectionStringBuilderAdapter : IDbConnectionStringBuilderAdapter
{
    private const string ConnectionStringNotSetMessage = "Connection string has not been set.";
    private NpgsqlConnectionStringBuilder? _builder;

    private NpgsqlConnectionStringBuilder Builder
    {
        get
        {
            if (_builder == null)
            {
                throw new InvalidOperationException(ConnectionStringNotSetMessage);
            }

            return _builder;
        }
    }

    public string ConnectionString
    {
        get => Builder.ConnectionString;
        set => _builder = new NpgsqlConnectionStringBuilder(value);
    }

    public string DatabaseName
    {
        get => Builder.Database ?? throw new InvalidOperationException("Database name has not been set.");
        set
        {
            Builder.Database = value;
        }
    }

    public string ServerName
    {
        get => Builder.Host ?? throw new InvalidOperationException("Server name has not been set.");
        set
        {
            Builder.Host = value;
        }
    }
}
