// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.OdsInstances.Manage;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances.Manage;

[TestFixture]
public class ReadOdsInstanceManageTests
{
    private static readonly IOptions<AppSettings> EnabledOptions = Options.Create(new AppSettings());

    [Test]
    public async Task GetOdsInstanceManages_ReturnsOkWithMappedList()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<OdsInstanceManage>
        {
            new OdsInstanceManage { Id = 1, Name = "Instance A", Status = "Pending", DatabaseTemplate = "Minimal" }
        };

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null)).Returns(queryResult);

        var result = await ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, queryParams, null, null, EnabledOptions);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceManageModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceManageModel>>;
        okResult!.Value.ShouldNotBeNull();
        okResult.Value.Count.ShouldBe(1);
        okResult.Value[0].Id.ShouldBe(1);
        okResult.Value[0].Name.ShouldBe("Instance A");
        okResult.Value[0].Status.ShouldBe("Pending");
        okResult.Value[0].DatabaseTemplate.ShouldBe("Minimal");
    }

    [Test]
    public async Task GetOdsInstanceManage_ReturnsOkWithMappedModel()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManageByIdQuery>();
        var queryResult = new OdsInstanceManage { Id = 5, Name = "Instance B", Status = "Completed", DatabaseTemplate = "Sample" };

        A.CallTo(() => fakeQuery.Execute(5)).Returns(queryResult);

        var result = await ReadOdsInstanceManage.GetOdsInstanceManage(fakeQuery, 5, EnabledOptions);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<OdsInstanceManageModel>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<OdsInstanceManageModel>;
        okResult!.Value.ShouldNotBeNull();
        okResult.Value.Id.ShouldBe(5);
        okResult.Value.Name.ShouldBe("Instance B");
        okResult.Value.Status.ShouldBe("Completed");
        okResult.Value.DatabaseTemplate.ShouldBe("Sample");
    }

    [Test]
    public void GetOdsInstanceManage_WhenNotFound_ThrowsNotFoundException()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManageByIdQuery>();

        A.CallTo(() => fakeQuery.Execute(99)).Returns(null);

        Should.Throw<NotFoundException<int>>(
            () => ReadOdsInstanceManage.GetOdsInstanceManage(fakeQuery, 99, EnabledOptions).GetAwaiter().GetResult());
    }

    [Test]
    public void GetOdsInstanceManages_WhenQueryThrows_ExceptionIsPropagated()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null))
            .Throws(new System.Exception("Query failed"));

        Should.Throw<System.Exception>(async () =>
            await ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, new CommonQueryParams(0, 10), null, null, EnabledOptions));
    }

    [Test]
    public async Task GetOdsInstanceManages_ReturnsOkWithEmptyList()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        var queryParams = new CommonQueryParams(0, 10);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null)).Returns(new List<OdsInstanceManage>());

        var result = await ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, queryParams, null, null, EnabledOptions);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceManageModel>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceManageModel>>;
        okResult!.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task GetOdsInstanceManages_WithIdFilter_PassesIdToQuery()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<OdsInstanceManage>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, 42, null)).Returns(queryResult);

        await ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, queryParams, 42, null, EnabledOptions);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, 42, null)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task GetOdsInstanceManages_WithNameFilter_PassesNameToQuery()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        var queryParams = new CommonQueryParams(0, 10);
        var queryResult = new List<OdsInstanceManage>();

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, "Instance A")).Returns(queryResult);

        await ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, queryParams, null, "Instance A", EnabledOptions);

        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, "Instance A")).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void GetOdsInstanceManages_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = Should.Throw<ValidationException>(() =>
            ReadOdsInstanceManage.GetOdsInstanceManages(fakeQuery, new CommonQueryParams(0, 10), null, null, disabledOptions).GetAwaiter().GetResult());

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }

    [Test]
    public void GetOdsInstanceManage_WhenDataStoreManagementDisabled_ThrowsValidationException()
    {
        var fakeQuery = A.Fake<IGetOdsInstanceManageByIdQuery>();
        var disabledOptions = Options.Create(new AppSettings { EnableDataStoreManagement = false });

        var exception = Should.Throw<ValidationException>(() =>
            ReadOdsInstanceManage.GetOdsInstanceManage(fakeQuery, 1, disabledOptions).GetAwaiter().GetResult());

        exception.Errors.Single().PropertyName.ShouldBe(nameof(OdsInstance));
    }
}
