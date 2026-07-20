// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores;

[TestFixture]
public class AddDataStoreTests
{
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "PostgreSql",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        });

    [Test]
    public async Task Handle_WithValidRequest_ReturnsCreated()
    {
        var fakeGetDataStores = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => fakeGetDataStores.Execute()).Returns(new List<OdsInstance>());
        var fakeAddCommand = A.Fake<IAddDataStoreCommand>();
        var ods = new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        A.CallTo(() => fakeAddCommand.Execute(A<IAddDataStoreModel>._)).Returns(ods);
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        var validator = new AddDataStore.Validator(fakeGetDataStores, Options());
        var request = new AddDataStore.AddDataStoreRequest
        {
            Name = "DS1",
            DataStoreType = "type",
            ConnectionString = "Host=localhost;Port=5432;Database=EdFi_ODS"
        };

        var fakeHttpContext = new DefaultHttpContext();
        fakeHttpContext.Request.Scheme = "https";
        fakeHttpContext.Request.Host = new HostString("localhost");

        var result = await AddDataStore.Handle(validator, fakeAddCommand, fakeEncryption, Options(), request, fakeHttpContext);

        result.ShouldNotBeNull();
    }

    [Test]
    public async Task Validator_WhenNameAlreadyExists_FailsValidation()
    {
        var fakeGetDataStores = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => fakeGetDataStores.Execute())
            .Returns(new List<OdsInstance> { new OdsInstance { Name = "Duplicate", InstanceType = "t", ConnectionString = "cs" } });
        var validator = new AddDataStore.Validator(fakeGetDataStores, Options());

        var result = await validator.ValidateAsync(new AddDataStore.AddDataStoreRequest { Name = "Duplicate", ConnectionString = "Host=localhost" });
        result.IsValid.ShouldBeFalse();
    }
}
