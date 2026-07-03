// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreContexts;

[TestFixture]
public class EditDataStoreContextHandlerTests
{
    [Test]
    public async Task Handle_WithValidRequest_ReturnsNoContent()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetContexts = A.Fake<IGetDataStoreContextsQuery>();
        A.CallTo(() => fakeGetContexts.Execute()).Returns(new List<OdsInstanceContext>());
        var fakeEditCommand = A.Fake<IEditDataStoreContextCommand>();

        var validator = new EditDataStoreContext.Validator(fakeGetDataStore, fakeGetContexts);
        var request = new EditDataStoreContext.EditDataStoreContextRequest
        {
            Id = 1, DataStoreId = 1, ContextKey = "key", ContextValue = "val"
        };

        var result = await EditDataStoreContext.Handle(validator, fakeEditCommand, request, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Test]
    public async Task Validator_WhenContextKeyEmpty_FailsValidation()
    {
        var fakeGetDataStore = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeGetDataStore.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "t", ConnectionString = "cs" });
        var fakeGetContexts = A.Fake<IGetDataStoreContextsQuery>();
        A.CallTo(() => fakeGetContexts.Execute()).Returns(new List<OdsInstanceContext>());

        var validator = new EditDataStoreContext.Validator(fakeGetDataStore, fakeGetContexts);
        var result = await validator.ValidateAsync(new EditDataStoreContext.EditDataStoreContextRequest { Id = 1, DataStoreId = 1, ContextKey = "", ContextValue = "v" });
        result.IsValid.ShouldBeFalse();
    }
}
