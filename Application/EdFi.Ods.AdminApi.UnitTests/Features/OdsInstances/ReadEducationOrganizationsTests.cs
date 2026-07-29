// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class ReadEducationOrganizationsTests
{
    private IGetEducationOrganizationsQuery _getEdOrgsQuery = null!;
    private IGetOdsInstanceManagesQuery _getOdsInstanceManagesQuery = null!;
    private IGetOdsInstanceQuery _getOdsInstanceQuery = null!;
    private CommonQueryParams _queryParams;

    [SetUp]
    public void SetUp()
    {
        _getEdOrgsQuery = A.Fake<IGetEducationOrganizationsQuery>();
        _getOdsInstanceManagesQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        _getOdsInstanceQuery = A.Fake<IGetOdsInstanceQuery>();
        _queryParams = new CommonQueryParams(0, 10);
    }

    [Test]
    public async Task GetEducationOrganizationsByInstance_DoesNotAppendUnlinkedOdsInstanceManages()
    {
        var instanceId = 3;
        A.CallTo(() => _getOdsInstanceQuery.Execute(instanceId)).Returns(new OdsInstance { OdsInstanceId = instanceId });
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, instanceId))
            .Returns(new List<OdsInstanceWithEducationOrganizationsModel>
            {
                new() { Id = instanceId, Name = "Instance3" }
            });
        A.CallTo(() => _getOdsInstanceManagesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<OdsInstanceManage>
            {
                new OdsInstanceManage { Id = 1, Name = "Unlinked", OdsInstanceId = null, Status = "PendingCreate" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizationsByInstance(
            _getEdOrgsQuery, _getOdsInstanceQuery, _getOdsInstanceManagesQuery, _queryParams, instanceId);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value!.Count.ShouldBe(1);
        ok.Value[0].Id.ShouldBe(instanceId);
    }

    [Test]
    public async Task GetEducationOrganizationsByInstance_EnrichesLinkedOdsInstanceManageFields()
    {
        var instanceId = 7;
        A.CallTo(() => _getOdsInstanceQuery.Execute(instanceId)).Returns(new OdsInstance { OdsInstanceId = instanceId });
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, instanceId))
            .Returns(new List<OdsInstanceWithEducationOrganizationsModel>
            {
                new() { Id = instanceId, Name = "Instance7" }
            });
        A.CallTo(() => _getOdsInstanceManagesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<OdsInstanceManage>
            {
                new OdsInstanceManage { Id = 5, OdsInstanceId = instanceId, Status = "Healthy", DatabaseTemplate = "Minimal", DatabaseName = "EdFi_Ods_7" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizationsByInstance(
            _getEdOrgsQuery, _getOdsInstanceQuery, _getOdsInstanceManagesQuery, _queryParams, instanceId);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value![0].OdsInstanceManageId.ShouldBe(5);
        ok.Value![0].Status.ShouldBe("Healthy");
        ok.Value[0].DatabaseTemplate.ShouldBe("Minimal");
        ok.Value[0].DatabaseName.ShouldBe("EdFi_Ods_7");
    }

    [Test]
    public void GetEducationOrganizationsByInstance_WhenOdsInstanceNotFound_ThrowsNotFoundException()
    {
        A.CallTo(() => _getOdsInstanceQuery.Execute(99))
            .Throws(new NotFoundException<int>("odsInstance", 99));

        Should.Throw<NotFoundException<int>>(async () =>
            await ReadEducationOrganizations.GetEducationOrganizationsByInstance(
                _getEdOrgsQuery, _getOdsInstanceQuery, _getOdsInstanceManagesQuery, _queryParams, 99));
    }
}
