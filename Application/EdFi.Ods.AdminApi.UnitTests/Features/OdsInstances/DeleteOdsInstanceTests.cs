// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class DeleteOdsInstanceTests
{
    [Test]
    public async Task Handle_WithValidOdsInstance_DeletesAndReturnsOk()
    {
        var fakeGetOdsInstance = A.Fake<IGetOdsInstanceQuery>();
        A.CallTo(() => fakeGetOdsInstance.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "ODS1", InstanceType = "type", ConnectionString = "cs" });

        var fakeGetAppsByOdsInstance = A.Fake<IGetApplicationsByOdsInstanceIdQuery>();
        A.CallTo(() => fakeGetAppsByOdsInstance.Execute(1)).Returns(new List<Application>());

        var fakeDeleteCommand = A.Fake<IDeleteOdsInstanceCommand>();
        var deleteOdsInstance = new DeleteOdsInstance();
        var validator = new DeleteOdsInstance.Validator(fakeGetOdsInstance, fakeGetAppsByOdsInstance);

        var result = await deleteOdsInstance.Handle(fakeDeleteCommand, validator, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<object>>();
        A.CallTo(() => fakeDeleteCommand.Execute(1)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Validator_WhenOdsInstanceHasApplications_FailsValidation()
    {
        var fakeGetOdsInstance = A.Fake<IGetOdsInstanceQuery>();
        A.CallTo(() => fakeGetOdsInstance.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "ODS1", InstanceType = "type", ConnectionString = "cs" });

        var fakeGetAppsByOdsInstance = A.Fake<IGetApplicationsByOdsInstanceIdQuery>();
        A.CallTo(() => fakeGetAppsByOdsInstance.Execute(1)).Returns(new List<Application> { new Application { ApplicationId = 1, ApplicationName = "App1" } });

        var validator = new DeleteOdsInstance.Validator(fakeGetOdsInstance, fakeGetAppsByOdsInstance);
        var result = await validator.ValidateAsync(new DeleteOdsInstance.Request { Id = 1 });

        result.IsValid.ShouldBeFalse();
    }
}
