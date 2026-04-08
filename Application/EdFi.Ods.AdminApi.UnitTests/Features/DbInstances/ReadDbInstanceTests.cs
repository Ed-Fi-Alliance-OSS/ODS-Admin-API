// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Features.DbInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.DbInstances;

[TestFixture]
public class ReadDbInstanceTests
{
    [Test]
    public async Task GetDbInstances_ReturnsOkWithMappedList()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<DbInstance>
        {
            new DbInstance { Id = 1, Name = "Instance A", Status = "Pending", DatabaseTemplate = "Minimal" }
        };

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null)).Returns(queryResult);

        var result = await ReadDbInstance.GetDbInstances(fakeQuery, queryParams, null, null);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>;
        okResult!.Value.ShouldNotBeNull();
        okResult.Value.Count.ShouldBe(1);
        okResult.Value[0].Id.ShouldBe(1);
        okResult.Value[0].Name.ShouldBe("Instance A");
        okResult.Value[0].Status.ShouldBe("Pending");
        okResult.Value[0].DatabaseTemplate.ShouldBe("Minimal");
    }

    [Test]
    public async Task GetDbInstance_ReturnsOkWithMappedModel()
    {
        var fakeQuery = A.Fake<IGetDbInstanceByIdQuery>();
        var queryResult = new DbInstance { Id = 5, Name = "Instance B", Status = "Completed", DatabaseTemplate = "Sample" };

        A.CallTo(() => fakeQuery.Execute(5)).Returns(queryResult);

        var result = await ReadDbInstance.GetDbInstance(fakeQuery, 5);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<DbInstanceModel>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<DbInstanceModel>;
        okResult!.Value.ShouldNotBeNull();
        okResult.Value.Id.ShouldBe(5);
        okResult.Value.Name.ShouldBe("Instance B");
        okResult.Value.Status.ShouldBe("Completed");
        okResult.Value.DatabaseTemplate.ShouldBe("Sample");
    }

    [Test]
    public void GetDbInstance_WhenNotFound_ThrowsNotFoundException()
    {
        var fakeQuery = A.Fake<IGetDbInstanceByIdQuery>();

        A.CallTo(() => fakeQuery.Execute(99)).Returns(null);

        Should.Throw<NotFoundException<int>>(
            () => ReadDbInstance.GetDbInstance(fakeQuery, 99).GetAwaiter().GetResult());
    }

    [Test]
    public void GetDbInstances_WhenQueryThrows_ExceptionIsPropagated()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null))
            .Throws(new System.Exception("Query failed"));

        Should.Throw<System.Exception>(async () =>
            await ReadDbInstance.GetDbInstances(fakeQuery, new CommonQueryParams(0, 10), null, null));
    }

    [Test]
    public async Task GetDbInstances_ReturnsOkWithEmptyList()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var queryParams = new CommonQueryParams(0, 10);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null)).Returns(new List<DbInstance>());

        var result = await ReadDbInstance.GetDbInstances(fakeQuery, queryParams, null, null);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>;
        okResult!.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task GetDbInstances_WithIdFilter_PassesIdToQuery()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<DbInstance>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, 42, null)).Returns(queryResult);

        await ReadDbInstance.GetDbInstances(fakeQuery, queryParams, 42, null);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, 42, null)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetDbInstances_WithNameFilter_PassesNameToQuery()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<DbInstance>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, "Instance A")).Returns(queryResult);

        await ReadDbInstance.GetDbInstances(fakeQuery, queryParams, null, "Instance A");

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, "Instance A")).MustHaveHappenedOnceExactly();
    }
}
