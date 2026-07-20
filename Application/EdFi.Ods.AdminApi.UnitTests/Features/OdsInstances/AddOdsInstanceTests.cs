// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class AddOdsInstanceTests
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
        var fakeGetOdsInstances = A.Fake<IGetOdsInstancesQuery>();
        A.CallTo(() => fakeGetOdsInstances.Execute()).Returns(new List<OdsInstance>());

        var fakeAddCommand = A.Fake<IAddOdsInstanceCommand>();
        var ods = new OdsInstance { OdsInstanceId = 42, Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        A.CallTo(() => fakeAddCommand.Execute(A<IAddOdsInstanceModel>._)).Returns(ods);

        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        A.CallTo(() => fakeEncryption.Encrypt(A<string>._, A<byte[]>._)).Returns("encrypted");

        // Use a connection string that passes Npgsql's basic validation
        const string validCs = "Host=localhost;Port=5432;Database=EdFi_ODS";

        var validator = new AddOdsInstance.Validator(fakeGetOdsInstances, Options());
        var request = new AddOdsInstance.AddOdsInstanceRequest
        {
            Name = "UniqueODS",
            InstanceType = "Shared",
            ConnectionString = validCs
        };

        var result = await AddOdsInstance.Handle(validator, fakeAddCommand, fakeEncryption, Options(), request);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created>();
    }

    [Test]
    public async Task Validator_WhenNameAlreadyExists_FailsValidation()
    {
        var fakeGetOdsInstances = A.Fake<IGetOdsInstancesQuery>();
        A.CallTo(() => fakeGetOdsInstances.Execute())
            .Returns(new List<OdsInstance> { new OdsInstance { Name = "Duplicate", InstanceType = "type", ConnectionString = "cs" } });

        var validator = new AddOdsInstance.Validator(fakeGetOdsInstances, Options());
        var result = await validator.ValidateAsync(new AddOdsInstance.AddOdsInstanceRequest { Name = "Duplicate", ConnectionString = "Host=localhost;Database=X" });

        result.IsValid.ShouldBeFalse();
    }
}
