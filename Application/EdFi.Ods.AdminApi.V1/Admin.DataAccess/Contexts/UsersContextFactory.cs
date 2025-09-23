// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Common;
using EdFi.Common.Configuration;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Providers;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts
{
    public class UsersContextFactory : IUsersContextFactory
    {
        private readonly Dictionary<DatabaseEngine, Type> _usersContextTypeByDatabaseEngine = new()
        {
            {DatabaseEngine.SqlServer, typeof(SqlServerUsersContext)},
            {DatabaseEngine.Postgres, typeof(PostgresUsersContext)}
        };

        private readonly DatabaseEngine _databaseEngine;

        private readonly IAdminDatabaseConnectionStringProvider _connectionStringsProvider;

        public UsersContextFactory(IAdminDatabaseConnectionStringProvider connectionStringsProvider, DatabaseEngine databaseEngine)
        {
            _connectionStringsProvider = Preconditions.ThrowIfNull(connectionStringsProvider, nameof(connectionStringsProvider));
            _databaseEngine = Preconditions.ThrowIfNull(databaseEngine, nameof(databaseEngine));
        }

        public Type GetUsersContextType()
        {
            if (_usersContextTypeByDatabaseEngine.TryGetValue(_databaseEngine, out Type? contextType) && contextType != null)
            {
                return contextType;
            }

            throw new InvalidOperationException(
                $"No UsersContext defined for database type {_databaseEngine.DisplayName}");
        }

        public IUsersContext CreateContext()
        {
            if (_databaseEngine == DatabaseEngine.SqlServer)
            {
                return Activator.CreateInstance(
                           GetUsersContextType(),
                           new DbContextOptionsBuilder<SqlServerUsersContext>()
                               .UseLazyLoadingProxies()
                               .UseSqlServer(_connectionStringsProvider.GetConnectionString())
                               .Options) as
                       IUsersContext ?? throw new InvalidOperationException("Failed to create SqlServerUsersContext instance.");
            }

            if (_databaseEngine == DatabaseEngine.Postgres)
            {
                return Activator.CreateInstance(
                           GetUsersContextType(),
                           new DbContextOptionsBuilder<PostgresUsersContext>()
                               .UseLazyLoadingProxies()
                               .UseNpgsql(_connectionStringsProvider.GetConnectionString())
                               .Options) as
                       IUsersContext ?? throw new InvalidOperationException("Failed to create PostgresUsersContext instance.");
            }

            throw new InvalidOperationException(
                $"Cannot create an IUsersContext for database type {_databaseEngine.DisplayName}");
        }
    }
}
