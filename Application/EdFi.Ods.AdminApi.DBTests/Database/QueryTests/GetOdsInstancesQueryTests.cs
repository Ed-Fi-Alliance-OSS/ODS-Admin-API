// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstancesQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetAllInstances()
    {
        Transaction(usersContext =>
        {
            CreateMultiple(2);
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var results = command.Execute();
            results.Count.ShouldBe(2);
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithOffsetAndLimit()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();
            var offset = 0;
            var limit = 2;

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");

            offset = 2;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 4");

            offset = 4;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 5");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithoutOffsetAndLimit()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 4");

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 5");
        });
    }
    [Test]
    public void ShouldGetAllInstancesWithoutLimit()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();
            var offset = 0;

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, null), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");

            offset = 2;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, null), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(3);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 4");

            offset = 4;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, null), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 5");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithoutOffset()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();
            var limit = 2;

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(null, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithId()
    {
        Transaction(usersContext =>
        {
            var odsInstances = CreateMultiple();
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), odsInstances[2].OdsInstanceId, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == odsInstances[2].Name);
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithName()
    {
        Transaction(usersContext =>
        {
            var odsInstances = CreateMultiple();
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings(), new Aes256SymmetricStringEncryptionProvider());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, odsInstances[2].Name, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == odsInstances[2].Name);
        });
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

    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private static readonly Aes256SymmetricStringEncryptionProvider EncryptionProvider = new();
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

    private static IOptions<AppSettings> OptionsWithKey(string? key = null) =>
        Options.Create(new AppSettings { EncryptionKey = key });

    [Test]
    public void ShouldEncryptUnencryptedConnectionStringsOnRead()
    {
        Transaction(usersContext =>
        {
            var odsInstances = new[]
            {
                new OdsInstance { Name = "enc-test-1", InstanceType = "type", ConnectionString = PlainConnectionString },
                new OdsInstance { Name = "enc-test-2", InstanceType = "type", ConnectionString = PlainConnectionString }
            };
            Save(odsInstances);

            var command = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), EncryptionProvider);
            var results = command.Execute();
            var encTestResults = results.Where(r => r.Name.StartsWith("enc-test-")).ToList();

            encTestResults.ShouldAllBe(r => EncryptionProvider.IsEncrypted(r.ConnectionString));
        });
    }

    [Test]
    public void ShouldNotReEncryptAlreadyEncryptedConnectionStringsOnRead()
    {
        Transaction(usersContext =>
        {
            var encrypted = EncryptionProvider.Encrypt(PlainConnectionString, new byte[32]);
            var odsInstance = new OdsInstance { Name = "no-reencrypt-test", InstanceType = "type", ConnectionString = encrypted };
            Save(odsInstance);

            var command = new GetOdsInstancesQuery(usersContext, OptionsWithKey(TestEncryptionKey), EncryptionProvider);
            var results = command.Execute();
            var result = results.Single(r => r.Name == "no-reencrypt-test");

            result.ConnectionString.ShouldBe(encrypted);
        });
    }
}
