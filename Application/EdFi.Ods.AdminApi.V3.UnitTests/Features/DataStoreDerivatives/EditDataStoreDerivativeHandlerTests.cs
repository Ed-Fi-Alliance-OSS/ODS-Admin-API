// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreDerivatives;

[TestFixture]
public class EditDataStoreDerivativeHandlerTests
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
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());
        var fakeEditCommand = A.Fake<IEditDataStoreDerivativeCommand>();

        var validator = new EditDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, Options());
        var request = new EditDataStoreDerivative.EditDataStoreDerivativeRequest
        {
            Id = 1, DataStoreId = 1, DerivativeType = "ReadReplica", ConnectionString = "Host=localhost;Port=5432;Database=EdFi"
        };

        var result = await EditDataStoreDerivative.Handle(validator, fakeEditCommand, request, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Test]
    public async Task Validator_WhenDerivativeTypeEmpty_FailsValidation()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetDerivatives = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeGetDerivatives.Execute()).Returns(new List<OdsInstanceDerivative>());

        var validator = new EditDataStoreDerivative.Validator(fakeGetDataStore, fakeGetDerivatives, Options());
        var result = await validator.ValidateAsync(new EditDataStoreDerivative.EditDataStoreDerivativeRequest { Id = 1, DataStoreId = 1, DerivativeType = "" });
        result.IsValid.ShouldBeFalse();
    }
}
