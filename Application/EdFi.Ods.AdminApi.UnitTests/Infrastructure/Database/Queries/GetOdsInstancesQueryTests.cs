// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstancesQueryTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstancesQuery_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> OptionsWithKey(string? key = null) =>
        Options.Create(new AppSettings
        {
            DatabaseEngine = DatabaseEngineEnum.PostgreSql,
            DefaultPageSizeLimit = 25,
            EncryptionKey = key
        });

    [Test]
    public void Execute_WithUnencryptedConnectionStrings_EncryptsAllOnRead()
    {
        using var usersContext = CreateContext();
        usersContext.OdsInstances.AddRange(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = PlainConnectionString },
            new OdsInstance { Name = "Instance2", InstanceType = "type", ConnectionString = PlainConnectionString });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), _provider);
        var results = query.Execute();

        results.ShouldAllBe(r => _provider.IsEncrypted(r.ConnectionString));
    }

    [Test]
    public void Execute_WithUnencryptedConnectionStrings_PersistsEncryptedValuesToDatabase()
    {
        using var usersContext = CreateContext();
        usersContext.OdsInstances.AddRange(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = PlainConnectionString },
            new OdsInstance { Name = "Instance2", InstanceType = "type", ConnectionString = PlainConnectionString });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), _provider);
        query.Execute();

        usersContext.OdsInstances.ToList()
            .ShouldAllBe(o => _provider.IsEncrypted(o.ConnectionString));
    }

    [Test]
    public void Execute_WithAlreadyEncryptedConnectionStrings_DoesNotReEncrypt()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        using var usersContext = CreateContext();
        usersContext.OdsInstances.AddRange(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = encrypted },
            new OdsInstance { Name = "Instance2", InstanceType = "type", ConnectionString = encrypted });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), _provider);
        var results = query.Execute();

        results.ShouldAllBe(r => r.ConnectionString == encrypted);
    }

    [Test]
    public void Execute_WithMixedConnectionStrings_OnlyEncryptsPlainText()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        using var usersContext = CreateContext();
        usersContext.OdsInstances.AddRange(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = PlainConnectionString },
            new OdsInstance { Name = "Instance2", InstanceType = "type", ConnectionString = encrypted });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), _provider);
        var results = query.Execute();

        results.ShouldAllBe(r => _provider.IsEncrypted(r.ConnectionString));
    }

    [Test]
    public void Execute_WithNullEncryptionKey_DoesNotEncryptAnyString()
    {
        using var usersContext = CreateContext();
        usersContext.OdsInstances.AddRange(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = PlainConnectionString },
            new OdsInstance { Name = "Instance2", InstanceType = "type", ConnectionString = PlainConnectionString });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(null), _provider);
        var results = query.Execute();

        results.ShouldAllBe(r => r.ConnectionString == PlainConnectionString);
    }

    [Test]
    public void Execute_WithParams_WithUnencryptedConnectionStrings_EncryptsOnRead()
    {
        using var usersContext = CreateContext();
        usersContext.OdsInstances.AddRange(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = PlainConnectionString },
            new OdsInstance { Name = "Instance2", InstanceType = "type", ConnectionString = PlainConnectionString });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), _provider);
        var results = query.Execute(new CommonQueryParams(0, 25), null, null, null);

        results.ShouldAllBe(r => _provider.IsEncrypted(r.ConnectionString));
    }

    [Test]
    public void Execute_WithEmptyConnectionString_ReturnsInstanceWithoutError()
    {
        using var usersContext = CreateContext();
        usersContext.OdsInstances.Add(
            new OdsInstance { Name = "Instance1", InstanceType = "type", ConnectionString = string.Empty });
        usersContext.SaveChanges();

        var query = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), _provider);
        var results = query.Execute();

        results.Count.ShouldBe(1);
        results[0].ConnectionString.ShouldBe(string.Empty);
    }
}
