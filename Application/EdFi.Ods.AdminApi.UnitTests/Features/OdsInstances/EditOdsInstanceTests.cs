// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class EditOdsInstanceTests
{
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "PostgreSql",
            EncryptionKey = Convert.ToBase64String(new byte[32])
        });

    [Test]
    public async Task Handle_WithValidRequest_ReturnsOk()
    {
        var fakeGetInstances = A.Fake<IGetOdsInstancesQuery>();
        A.CallTo(() => fakeGetInstances.Execute()).Returns(new List<OdsInstance>());
        var fakeGetInstance = A.Fake<IGetOdsInstanceQuery>();
        A.CallTo(() => fakeGetInstance.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "ODS1", InstanceType = "type", ConnectionString = "cs" });
        var fakeEditCommand = A.Fake<IEditOdsInstanceCommand>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        var validator = new EditOdsInstance.Validator(fakeGetInstances, fakeGetInstance, Options());
        var request = new EditOdsInstance.EditOdsInstanceRequest { Name = "UpdatedODS", InstanceType = "type" };
        request.Id = 1;

        var result = await EditOdsInstance.Handle(validator, fakeEditCommand, fakeEncryption, Options(), request, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();
    }

    [Test]
    public async Task Validator_WhenNameEmpty_FailsValidation()
    {
        var fakeGetInstances = A.Fake<IGetOdsInstancesQuery>();
        A.CallTo(() => fakeGetInstances.Execute()).Returns(new List<OdsInstance>());
        var fakeGetInstance = A.Fake<IGetOdsInstanceQuery>();
        A.CallTo(() => fakeGetInstance.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "ODS1", InstanceType = "type", ConnectionString = "cs" });

        var validator = new EditOdsInstance.Validator(fakeGetInstances, fakeGetInstance, Options());
        var result = await validator.ValidateAsync(new EditOdsInstance.EditOdsInstanceRequest { Name = "", Id = 1 });
        result.IsValid.ShouldBeFalse();
    }
}
