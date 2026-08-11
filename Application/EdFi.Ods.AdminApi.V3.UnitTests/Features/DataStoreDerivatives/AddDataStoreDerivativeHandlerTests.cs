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
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreDerivatives;

[TestFixture]
public class AddDataStoreDerivativeHandlerTests
{
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "PostgreSql",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        });

    [Test]
    public async Task Handle_WithValidRequest_EncryptsConnectionStringBeforeExecute()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeAddCommand = A.Fake<IAddDataStoreDerivativeCommand>();
        var derivative = new OdsInstanceDerivative { OdsInstanceDerivativeId = 5, DerivativeType = "ReadReplica", OdsInstance = new OdsInstance { OdsInstanceId = 1 } };
        string? capturedConnectionStringAtCallTime = null;
        A.CallTo(() => fakeAddCommand.Execute(A<IAddDataStoreDerivativeModel>._))
            .Invokes((IAddDataStoreDerivativeModel m) => capturedConnectionStringAtCallTime = m.ConnectionString)
            .Returns(derivative);
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        var validator = new AddDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, Options());
        var request = new AddDataStoreDerivative.AddDataStoreDerivativeRequest
        {
            DataStoreId = 1,
            DerivativeType = "ReadReplica",
            ConnectionString = "Host=localhost;Port=5432;Database=EdFi_ODS"
        };

        var fakeHttpContext = new DefaultHttpContext();
        fakeHttpContext.Request.Scheme = "https";
        fakeHttpContext.Request.Host = new HostString("localhost");

        var result = await AddDataStoreDerivative.Handle(validator, fakeAddCommand, fakeEncryption, Options(), request, fakeHttpContext);

        request.ConnectionString.ShouldBe("encrypted");
        capturedConnectionStringAtCallTime.ShouldBe("encrypted");
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_WithNullEncryptionKey_ThrowsInvalidOperationException()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeAddCommand = A.Fake<IAddDataStoreDerivativeCommand>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        var optionsWithoutKey = Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql", EncryptionKey = null });

        var validator = new AddDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, optionsWithoutKey);
        var request = new AddDataStoreDerivative.AddDataStoreDerivativeRequest
        {
            DataStoreId = 1,
            DerivativeType = "ReadReplica",
            ConnectionString = "Host=localhost;Port=5432;Database=EdFi_ODS"
        };
        var fakeHttpContext = new DefaultHttpContext();

        await Should.ThrowAsync<InvalidOperationException>(
            () => AddDataStoreDerivative.Handle(validator, fakeAddCommand, fakeEncryption, optionsWithoutKey, request, fakeHttpContext));
    }
}
