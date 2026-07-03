// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores;

[TestFixture]
public class DeleteDataStoreTests
{
    [Test]
    public async Task Handle_WithNoAssociations_ReturnsNoContent()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        var ods = new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs",
            OdsInstanceContexts = new List<OdsInstanceContext>(),
            OdsInstanceDerivatives = new List<OdsInstanceDerivative>() };
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(ods);
        var fakeGetApps = A.Fake<IGetApplicationsByDataStoreIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<Application>());
        var fakeDeleteCommand = A.Fake<IDeleteDataStoreCommand>();

        var deleteDataStore = new DeleteDataStore();
        var validator = new DeleteDataStore.Validator(fakeGetDataStore, fakeGetApps);

        var result = await deleteDataStore.Handle(fakeDeleteCommand, validator, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        A.CallTo(() => fakeDeleteCommand.Execute(1)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Validator_WhenDataStoreHasApplications_FailsValidation()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs",
            OdsInstanceContexts = new List<OdsInstanceContext>(),
            OdsInstanceDerivatives = new List<OdsInstanceDerivative>() });
        var fakeGetApps = A.Fake<IGetApplicationsByDataStoreIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<Application> { new Application { ApplicationId = 1, ApplicationName = "App1" } });

        var validator = new DeleteDataStore.Validator(fakeGetDataStore, fakeGetApps);
        var result = await validator.ValidateAsync(new DeleteDataStore.Request { Id = 1 });
        result.IsValid.ShouldBeFalse();
    }
}
