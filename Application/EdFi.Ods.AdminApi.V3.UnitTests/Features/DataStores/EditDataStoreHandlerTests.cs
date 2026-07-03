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
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores;

[TestFixture]
public class EditDataStoreHandlerTests
{
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "PostgreSql",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        });

    [Test]
    public async Task Handle_WithValidRequest_ReturnsNoContent()
    {
        var fakeGetDataStores = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => fakeGetDataStores.Execute()).Returns(new List<OdsInstance>());
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "type", ConnectionString = "cs" });
        var fakeEditCommand = A.Fake<IEditDataStoreCommand>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        var validator = new EditDataStore.Validator(fakeGetDataStores, fakeGetDataStore, Options());
        var request = new EditDataStore.EditDataStoreRequest { Name = "DS1", DataStoreType = "type", Id = 1 };

        var result = await EditDataStore.Handle(validator, fakeEditCommand, fakeEncryption, Options(), request, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Test]
    public async Task Validator_WhenNameEmpty_FailsValidation()
    {
        var fakeGetDataStores = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => fakeGetDataStores.Execute()).Returns(new List<OdsInstance>());
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "type", ConnectionString = "cs" });

        var validator = new EditDataStore.Validator(fakeGetDataStores, fakeGetDataStore, Options());
        var result = await validator.ValidateAsync(new EditDataStore.EditDataStoreRequest { Name = "", Id = 1 });
        result.IsValid.ShouldBeFalse();
    }
}
