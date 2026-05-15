// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceQueryTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"V3_GetOdsInstanceQuery_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> OptionsWithKey(string? key = null) =>
        Options.Create(new AppSettings { EncryptionKey = key });

    [Test]
    public void Execute_WithUnencryptedConnectionString_EncryptsOnRead()
    {
        using var usersContext = CreateContext();
        var odsInstance = new OdsInstance { Name = "Test", InstanceType = "type", ConnectionString = PlainConnectionString };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();

        var query = new GetOdsInstanceQuery(usersContext, _provider, OptionsWithKey(TestEncryptionKey));
        var result = query.Execute(odsInstance.OdsInstanceId);

        result.ConnectionString.ShouldNotBe(PlainConnectionString);
        _provider.IsEncrypted(result.ConnectionString).ShouldBeTrue();
    }

    [Test]
    public void Execute_WithUnencryptedConnectionString_PersistsEncryptedValueToDatabase()
    {
        using var usersContext = CreateContext();
        var odsInstance = new OdsInstance { Name = "Test", InstanceType = "type", ConnectionString = PlainConnectionString };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();

        var query = new GetOdsInstanceQuery(usersContext, _provider, OptionsWithKey(TestEncryptionKey));
        query.Execute(odsInstance.OdsInstanceId);

        var persisted = usersContext.OdsInstances.Find(odsInstance.OdsInstanceId);
        _provider.IsEncrypted(persisted!.ConnectionString).ShouldBeTrue();
    }

    [Test]
    public void Execute_WithAlreadyEncryptedConnectionString_DoesNotReEncrypt()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        using var usersContext = CreateContext();
        var odsInstance = new OdsInstance { Name = "Test", InstanceType = "type", ConnectionString = encrypted };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();

        var query = new GetOdsInstanceQuery(usersContext, _provider, OptionsWithKey(TestEncryptionKey));
        var result = query.Execute(odsInstance.OdsInstanceId);

        result.ConnectionString.ShouldBe(encrypted);
    }

    [Test]
    public void Execute_WithNullEncryptionKey_DoesNotEncrypt()
    {
        using var usersContext = CreateContext();
        var odsInstance = new OdsInstance { Name = "Test", InstanceType = "type", ConnectionString = PlainConnectionString };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();

        var query = new GetOdsInstanceQuery(usersContext, _provider, OptionsWithKey(null));
        var result = query.Execute(odsInstance.OdsInstanceId);

        result.ConnectionString.ShouldBe(PlainConnectionString);
    }

    [Test]
    public void Execute_WithEmptyConnectionString_ReturnsInstanceWithoutError()
    {
        using var usersContext = CreateContext();
        var odsInstance = new OdsInstance { Name = "Test", InstanceType = "type", ConnectionString = string.Empty };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();

        var query = new GetOdsInstanceQuery(usersContext, _provider, OptionsWithKey(TestEncryptionKey));
        var result = query.Execute(odsInstance.OdsInstanceId);

        result.ConnectionString.ShouldBe(string.Empty);
    }
}
