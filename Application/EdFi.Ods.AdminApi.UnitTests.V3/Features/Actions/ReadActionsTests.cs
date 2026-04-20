// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.Actions;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Models;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V3.Features.Actions;

[TestFixture]
public class ReadActionsTests
{
    [Test]
    public async Task GetActions_ReturnsOkWithMappedList()
    {
        var fakeQuery = A.Fake<IGetAllActionsQuery>();

        var queryResult = new List<Action>
        {
            new Action { ActionId = 1, ActionName = "Read", ActionUri = "/resource/read" }
        };

        A.CallTo(() => fakeQuery.Execute(A<EdFi.Ods.AdminApi.Common.Infrastructure.CommonQueryParams>._, null, null)).Returns(queryResult);

        var result = await ReadActions.GetActions(fakeQuery, 0, 10, "name", "Ascending", null, null);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ActionModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<ActionModel>>;
        okResult!.Value.ShouldNotBeNull();
        okResult.Value.Count.ShouldBe(1);
        okResult.Value[0].Id.ShouldBe(1);
        okResult.Value[0].Name.ShouldBe("Read");
        okResult.Value[0].Uri.ShouldBe("/resource/read");
    }

    [Test]
    public void GetActions_WhenQueryThrows_ExceptionIsPropagated()
    {
        var fakeQuery = A.Fake<IGetAllActionsQuery>();

        A.CallTo(() => fakeQuery.Execute(A<EdFi.Ods.AdminApi.Common.Infrastructure.CommonQueryParams>._, null, null))
            .Throws(new System.Exception("Query failed"));

        Should.Throw<System.Exception>(async () => await ReadActions.GetActions(fakeQuery, 0, 10, null, null, null, null));
    }
}


