// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstanceByIdQueryTests : PlatformUsersContextTestBase
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private static readonly Aes256SymmetricStringEncryptionProvider EncryptionProvider = new();
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

    private static IOptions<AppSettings> OptionsWithKey(string? key = null) =>
        Options.Create(new AppSettings { EncryptionKey = key });

    [Test]
    public void ShouldGetInstanceById()
    {
        Transaction(usersContext =>
        {
            var odsInstance = new OdsInstance
            {
                InstanceType = "test type",
                Name = "test ods instance 1",
                ConnectionString = PlainConnectionString
            };
            Save(odsInstance);
            var command = new GetOdsInstanceQuery(usersContext, EncryptionProvider, Testing.GetAppSettings());
            var result = command.Execute(odsInstance.OdsInstanceId);
            result.OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);
            result.Name.ShouldBe("test ods instance 1");
        });
    }

    [Test]
    public void ShouldEncryptUnencryptedConnectionStringOnRead()
    {
        Transaction(usersContext =>
        {
            var odsInstance = new OdsInstance
            {
                InstanceType = "test type",
                Name = "test encrypt on read",
                ConnectionString = PlainConnectionString
            };
            Save(odsInstance);

            var command = new GetOdsInstanceQuery(usersContext, EncryptionProvider, OptionsWithKey(TestEncryptionKey));
            var result = command.Execute(odsInstance.OdsInstanceId);

            result.ConnectionString.ShouldNotBe(PlainConnectionString);
            EncryptionProvider.IsEncrypted(result.ConnectionString).ShouldBeTrue();
        });
    }

    [Test]
    public void ShouldNotReEncryptAlreadyEncryptedConnectionString()
    {
        Transaction(usersContext =>
        {
            var encrypted = EncryptionProvider.Encrypt(PlainConnectionString, new byte[32]);
            var odsInstance = new OdsInstance
            {
                InstanceType = "test type",
                Name = "test no re-encrypt",
                ConnectionString = encrypted
            };
            Save(odsInstance);

            var command = new GetOdsInstanceQuery(usersContext, EncryptionProvider, OptionsWithKey(TestEncryptionKey));
            var result = command.Execute(odsInstance.OdsInstanceId);

            result.ConnectionString.ShouldBe(encrypted);
        });
    }
}


