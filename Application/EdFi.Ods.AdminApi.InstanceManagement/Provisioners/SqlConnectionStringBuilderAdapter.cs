// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.Data.SqlClient;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

/// <summary>
/// Implements a SQL Server-specific adapter for the <see cref="System.Data.Common.DbConnectionStringBuilder" />
/// that provides the database name through the <see cref="SqlConnectionStringBuilder.InitialCatalog" />
/// property and the server name through <see cref="SqlConnectionStringBuilder.DataSource" />.
/// </summary>
public class SqlConnectionStringBuilderAdapter : IDbConnectionStringBuilderAdapter
{
    private const string ConnectionStringNotSetMessage = "Connection string has not been set.";
    private SqlConnectionStringBuilder? _builder;

    private SqlConnectionStringBuilder Builder
    {
        get
        {
            if (_builder is null)
            {
                throw new InvalidOperationException(ConnectionStringNotSetMessage);
            }

            return _builder;
        }
    }

    public string ConnectionString
    {
        get => Builder.ConnectionString;
        set => _builder = new SqlConnectionStringBuilder(value);
    }

    public string DatabaseName
    {
        get => Builder.InitialCatalog;
        set => Builder.InitialCatalog = value;
    }

    public string ServerName
    {
        get => Builder.DataSource;
        set => Builder.DataSource = value;
    }
}
