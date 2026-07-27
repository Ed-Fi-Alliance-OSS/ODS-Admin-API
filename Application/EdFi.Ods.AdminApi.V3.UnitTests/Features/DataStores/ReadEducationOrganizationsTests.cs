// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores;

[TestFixture]
public class ReadEducationOrganizationsTests
{
    private IGetEducationOrganizationsQuery _getEdOrgsQuery = null!;
    private IGetDbDataStoresQuery _getDbDataStoresQuery = null!;
    private IGetDataStoreQuery _getDataStoreQuery = null!;
    private CommonQueryParams _queryParams;

    [SetUp]
    public void SetUp()
    {
        _getEdOrgsQuery = A.Fake<IGetEducationOrganizationsQuery>();
        _getDbDataStoresQuery = A.Fake<IGetDbDataStoresQuery>();
        _getDataStoreQuery = A.Fake<IGetDataStoreQuery>();
        _queryParams = new CommonQueryParams(0, 10);
    }

    [Test]
    public async Task GetEducationOrganizationsByDataStore_DoesNotAppendUnlinkedDbDataStores()
    {
        var dataStoreId = 3;
        A.CallTo(() => _getDataStoreQuery.Execute(dataStoreId)).Returns(new EdFi.Admin.DataAccess.Models.OdsInstance { OdsInstanceId = dataStoreId });
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, dataStoreId))
            .Returns(new List<DataStoreWithEducationOrganizationsModel>
            {
                new() { Id = dataStoreId, Name = "DataStore3" }
            });
        A.CallTo(() => _getDbDataStoresQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>
            {
                new DbInstance { Id = 1, Name = "Unlinked", OdsInstanceId = null, Status = "PendingCreate" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizationsByDataStore(
            _getEdOrgsQuery, _getDataStoreQuery, _getDbDataStoresQuery, _queryParams, dataStoreId);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<DataStoreWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value!.Count.ShouldBe(1);
        ok.Value[0].Id.ShouldBe(dataStoreId);
    }

    [Test]
    public async Task GetEducationOrganizationsByDataStore_EnrichesLinkedDbDataStoreFields()
    {
        var dataStoreId = 7;
        A.CallTo(() => _getDataStoreQuery.Execute(dataStoreId)).Returns(new EdFi.Admin.DataAccess.Models.OdsInstance { OdsInstanceId = dataStoreId });
        A.CallTo(() => _getEdOrgsQuery.ExecuteAsync(_queryParams, dataStoreId))
            .Returns(new List<DataStoreWithEducationOrganizationsModel>
            {
                new() { Id = dataStoreId, Name = "DataStore7" }
            });
        A.CallTo(() => _getDbDataStoresQuery.Execute(A<CommonQueryParams>._, null, null))
            .Returns(new List<DbInstance>
            {
                new DbInstance { Id = 5, OdsInstanceId = dataStoreId, Status = "Healthy", DatabaseTemplate = "Minimal", DatabaseName = "EdFi_Ods_7" }
            });

        var result = await ReadEducationOrganizations.GetEducationOrganizationsByDataStore(
            _getEdOrgsQuery, _getDataStoreQuery, _getDbDataStoresQuery, _queryParams, dataStoreId);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<DataStoreWithEducationOrganizationsModel>>;
        ok.ShouldNotBeNull();
        ok.Value![0].Status.ShouldBe("Healthy");
        ok.Value[0].DatabaseTemplate.ShouldBe("Minimal");
        ok.Value[0].DatabaseName.ShouldBe("EdFi_Ods_7");
    }

    [Test]
    public void GetEducationOrganizationsByDataStore_WhenDataStoreNotFound_ThrowsNotFoundException()
    {
        A.CallTo(() => _getDataStoreQuery.Execute(99))
            .Throws(new NotFoundException<int>("DataStore", 99));

        Should.Throw<NotFoundException<int>>(async () =>
            await ReadEducationOrganizations.GetEducationOrganizationsByDataStore(
                _getEdOrgsQuery, _getDataStoreQuery, _getDbDataStoresQuery, _queryParams, 99));
    }
}
