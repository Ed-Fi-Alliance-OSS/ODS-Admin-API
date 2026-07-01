// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;
using Constants = EdFi.Ods.AdminApi.Common.Constants.Constants;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class ReadEducationOrganizationsTests
{
    private IGetEducationOrganizationsQuery _getEdOrgsQuery = null!;
    private IGetDbInstancesQuery _getDbInstancesQuery = null!;
    private IGetOdsInstanceQuery _getOdsInstanceQuery = null!;
    private CommonQueryParams _queryParams;

    [SetUp]
    public void SetUp()
    {
        _getEdOrgsQuery = A.Fake<IGetEducationOrganizationsQuery>();
        _getDbInstancesQuery = A.Fake<IGetDbInstancesQuery>();
        _getOdsInstanceQuery = A.Fake<IGetOdsInstanceQuery>();
        _queryParams = new CommonQueryParams(0, 10);
    }

    [Test]
    public async Task GetEducationOrganizations_ReturnsOk_WithLinkedDbInstanceFields()
    {
        var instances = new List<OdsInstanceWithEducationOrganizationsModel>
        {
            new() { Id = 1, Name = "Instance1" }
        };
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, null))
            .Returns(instances);
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>
            {
                new DbInstance { Id = 10, OdsInstanceId = 1, Status = "Healthy", DatabaseTemplate = "Minimal", DatabaseName = "EdFi_Ods" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizations(_getEdOrgsQuery, _getDbInstancesQuery, _queryParams);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value!.Count.ShouldBe(1);
        ok.Value[0].Status.ShouldBe("Healthy");
        ok.Value[0].DatabaseTemplate.ShouldBe("Minimal");
        ok.Value[0].DatabaseName.ShouldBe("EdFi_Ods");
    }

    [Test]
    public async Task GetEducationOrganizations_SetsCreatedStatus_WhenNoMatchingDbInstance()
    {
        var instances = new List<OdsInstanceWithEducationOrganizationsModel>
        {
            new() { Id = 5, Name = "Unmatched" }
        };
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, null))
            .Returns(instances);
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>());

        var result = await ReadEducationOrganizations.GetEducationOrganizations(_getEdOrgsQuery, _getDbInstancesQuery, _queryParams);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value![0].Status.ShouldBe(DbInstanceStatus.Created.ToString());
        ok.Value[0].DatabaseTemplate.ShouldBeNull();
        ok.Value[0].DatabaseName.ShouldBeNull();
    }

    [Test]
    public async Task GetEducationOrganizations_AppendsUnlinkedDbInstances_WithNegativeIds()
    {
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, null))
            .Returns(new List<OdsInstanceWithEducationOrganizationsModel>());
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>
            {
                new DbInstance { Id = 1, Name = "Unlinked-A", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Sample", DatabaseName = "EdFi_Ods_1" },
                new DbInstance { Id = 2, Name = "Unlinked-B", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Minimal", DatabaseName = "EdFi_Ods_2" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizations(_getEdOrgsQuery, _getDbInstancesQuery, _queryParams);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value!.Count.ShouldBe(2);
        ok.Value[0].Id.ShouldBe(-1);
        ok.Value[0].Name.ShouldBe("Unlinked-A");
        ok.Value[1].Id.ShouldBe(-2);
        ok.Value[1].Name.ShouldBe("Unlinked-B");
        ok.Value.ShouldAllBe(i => i.EducationOrganizations.Count == 0);
    }

    [Test]
    public async Task GetEducationOrganizationsByInstance_DoesNotAppendUnlinkedDbInstances()
    {
        var instanceId = 3;
        A.CallTo(() => _getOdsInstanceQuery.Execute(instanceId)).Returns(new OdsInstance { OdsInstanceId = instanceId });
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, instanceId))
            .Returns(new List<OdsInstanceWithEducationOrganizationsModel>
            {
                new() { Id = instanceId, Name = "Instance3" }
            });
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>
            {
                new DbInstance { Id = 1, Name = "Unlinked", OdsInstanceId = null, Status = "PendingCreate" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizationsByInstance(
            _getEdOrgsQuery, _getOdsInstanceQuery, _getDbInstancesQuery, _queryParams, instanceId);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value!.Count.ShouldBe(1);
        ok.Value[0].Id.ShouldBe(instanceId);
    }

    [Test]
    public async Task GetEducationOrganizationsByInstance_EnrichesLinkedDbInstanceFields()
    {
        var instanceId = 7;
        A.CallTo(() => _getOdsInstanceQuery.Execute(instanceId)).Returns(new OdsInstance { OdsInstanceId = instanceId });
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, instanceId))
            .Returns(new List<OdsInstanceWithEducationOrganizationsModel>
            {
                new() { Id = instanceId, Name = "Instance7" }
            });
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>
            {
                new DbInstance { Id = 5, OdsInstanceId = instanceId, Status = "Healthy", DatabaseTemplate = "Minimal", DatabaseName = "EdFi_Ods_7" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizationsByInstance(
            _getEdOrgsQuery, _getOdsInstanceQuery, _getDbInstancesQuery, _queryParams, instanceId);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<OdsInstanceWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
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
                _getEdOrgsQuery, _getOdsInstanceQuery, _getDbInstancesQuery, _queryParams, 99));
    }
}
