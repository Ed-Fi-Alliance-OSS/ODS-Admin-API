// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstancesQueryByTenantTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetAllInstancesWithSqlServerEngine()
    {
        Transaction(usersContext =>
        {
            var odsInstances = CreateMultiple(3);
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());

            var databaseEngine = DatabaseEngineEnum.SqlServer;
            var adminConnectionString = Testing.AdminConnectionString;

            var results = command.Execute(databaseEngine, adminConnectionString);

            results.ShouldNotBeNull();
            results.ShouldNotBeEmpty();
            results.Count.ShouldBe(3);

            results[0].Name.ShouldBe(odsInstances[0].Name);
            results[1].Name.ShouldBe(odsInstances[1].Name);
            results[2].Name.ShouldBe(odsInstances[2].Name);
        });
    }

    [Test]
    public void ShouldThrowNotSupportedExceptionForUnsupportedDatabaseEngine()
    {
        Transaction(usersContext =>
        {
            CreateMultiple(2);
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());

            var databaseEngine = "UnsupportedEngine";
            var adminConnectionString = Testing.AdminConnectionString;

            Should.Throw<NotSupportedException>(() =>
            {
                command.Execute(databaseEngine, adminConnectionString);
            }).Message.ShouldBe($"Database engine '{databaseEngine}' is not supported.");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithPostgreSqlEngine()
    {
        var postgreSqlConnectionString = GetPostgreSqlConnectionString();
        if (string.IsNullOrEmpty(postgreSqlConnectionString))
        {
            Assert.Ignore("PostgreSQL connection string not configured");
        }

        Transaction(usersContext =>
        {
            var odsInstances = CreateMultiple(3);
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());

            var databaseEngine = DatabaseEngineEnum.PostgreSql;

            var results = command.Execute(databaseEngine, postgreSqlConnectionString);

            results.ShouldNotBeNull();
            results.ShouldNotBeEmpty();
            results.Count.ShouldBe(3);

            results[0].Name.ShouldBe(odsInstances[0].Name);
            results[1].Name.ShouldBe(odsInstances[1].Name);
            results[2].Name.ShouldBe(odsInstances[2].Name);
        });
    }

    [Test]
    public void ShouldGetEmptyListWhenNoInstancesExist()
    {
        Transaction(usersContext =>
        {
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var databaseEngine = DatabaseEngineEnum.PostgreSql;
            var adminConnectionString = Testing.AdminConnectionString;

            var results = command.Execute(databaseEngine, adminConnectionString);

            results.ShouldNotBeNull();
            results.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldHandleNullOrEmptyDatabaseEngineGracefully()
    {
        Transaction(usersContext =>
        {
            CreateMultiple(2);
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var adminConnectionString = Testing.AdminConnectionString;

            Should.Throw<NotSupportedException>(() =>
            {
                command.Execute(string.Empty, adminConnectionString);
            });

            Should.Throw<NotSupportedException>(() =>
            {
                command.Execute(null!, adminConnectionString);
            });
        });
    }

    private static string GetPostgreSqlConnectionString()
    {
        var configuration = Testing.Configuration();
        return configuration.GetConnectionString("EdFi_Admin_PostgreSql");
    }

    private static OdsInstance[] CreateMultiple(int total = 5)
    {
        var odsInstances = new OdsInstance[total];

        for (var odsIndex = 0; odsIndex < total; odsIndex++)
        {
            odsInstances[odsIndex] = new OdsInstance
            {
                InstanceType = "test type",
                Name = $"test ods instance {odsIndex + 1}",
                ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
            };
        }

        Save(odsInstances);

        return odsInstances;
    }
}
