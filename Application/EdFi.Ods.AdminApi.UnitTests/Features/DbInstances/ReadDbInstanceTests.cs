// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
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
        var fakeMapper = A.Fake<IMapper>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<DbInstance>
        {
            new DbInstance { Id = 1, Name = "Instance A", Status = "Pending", DatabaseTemplate = "Minimal" }
        };
        var mappedResult = new List<DbInstanceModel>
        {
            new DbInstanceModel { Id = 1, Name = "Instance A", Status = "Pending", DatabaseTemplate = "Minimal" }
        };

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null)).Returns(queryResult);
        A.CallTo(() => fakeMapper.Map<List<DbInstanceModel>>(queryResult)).Returns(mappedResult);

        var result = await ReadDbInstance.GetDbInstances(fakeQuery, fakeMapper, queryParams, null, null);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>;
        okResult!.Value.ShouldBe(mappedResult);
    }

    [Test]
    public async Task GetDbInstance_ReturnsOkWithMappedModel()
    {
        var fakeQuery = A.Fake<IGetDbInstanceByIdQuery>();
        var fakeMapper = A.Fake<IMapper>();
        var queryResult = new DbInstance { Id = 5, Name = "Instance B", Status = "Completed", DatabaseTemplate = "Sample" };
        var mappedResult = new DbInstanceModel { Id = 5, Name = "Instance B", Status = "Completed", DatabaseTemplate = "Sample" };

        A.CallTo(() => fakeQuery.Execute(5)).Returns(queryResult);
        A.CallTo(() => fakeMapper.Map<DbInstanceModel>(queryResult)).Returns(mappedResult);

        var result = await ReadDbInstance.GetDbInstance(fakeQuery, fakeMapper, 5);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<DbInstanceModel>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<DbInstanceModel>;
        okResult!.Value.ShouldBe(mappedResult);
    }

    [Test]
    public void GetDbInstance_WhenNotFound_ThrowsNotFoundException()
    {
        var fakeQuery = A.Fake<IGetDbInstanceByIdQuery>();
        var fakeMapper = A.Fake<IMapper>();

        A.CallTo(() => fakeQuery.Execute(99)).Returns(null);

        Should.Throw<NotFoundException<int>>(
            () => ReadDbInstance.GetDbInstance(fakeQuery, fakeMapper, 99).GetAwaiter().GetResult());
    }

    [Test]
    public void GetDbInstances_WhenQueryThrows_ExceptionIsPropagated()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var fakeMapper = A.Fake<IMapper>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null))
            .Throws(new System.Exception("Query failed"));

        Should.Throw<System.Exception>(async () =>
            await ReadDbInstance.GetDbInstances(fakeQuery, fakeMapper, new CommonQueryParams(0, 10), null, null));
    }

    [Test]
    public async Task GetDbInstances_ReturnsOkWithEmptyList()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var fakeMapper = A.Fake<IMapper>();
        var queryParams = new CommonQueryParams(0, 10);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null)).Returns(new List<DbInstance>());
        A.CallTo(() => fakeMapper.Map<List<DbInstanceModel>>(A<List<DbInstance>>._)).Returns(new List<DbInstanceModel>());

        var result = await ReadDbInstance.GetDbInstances(fakeQuery, fakeMapper, queryParams, null, null);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<DbInstanceModel>>;
        okResult!.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task GetDbInstances_WithIdFilter_PassesIdToQuery()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var fakeMapper = A.Fake<IMapper>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<DbInstance>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, 42, null)).Returns(queryResult);
        A.CallTo(() => fakeMapper.Map<List<DbInstanceModel>>(queryResult)).Returns(new List<DbInstanceModel>());

        await ReadDbInstance.GetDbInstances(fakeQuery, fakeMapper, queryParams, 42, null);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, 42, null)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetDbInstances_WithNameFilter_PassesNameToQuery()
    {
        var fakeQuery = A.Fake<IGetDbInstancesQuery>();
        var fakeMapper = A.Fake<IMapper>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<DbInstance>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, "Instance A")).Returns(queryResult);
        A.CallTo(() => fakeMapper.Map<List<DbInstanceModel>>(queryResult)).Returns(new List<DbInstanceModel>());

        await ReadDbInstance.GetDbInstances(fakeQuery, fakeMapper, queryParams, null, "Instance A");

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, "Instance A")).MustHaveHappenedOnceExactly();
    }
}
