// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores;
[TestFixture] public class ReadDataStoreTests {
    [Test] public async Task GetDataStores_ReturnsOkWithList() {
        var fakeQuery = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null, null, null)).Returns(new List<OdsInstance> {
            new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "type", ConnectionString = "cs" }
        });
        var result = await ReadDataStore.GetDataStores(fakeQuery, new CommonQueryParams(0,25), null, null, null);
        result.ShouldNotBeNull();
    }
    [Test] public async Task GetDataStore_WhenFound_ReturnsOk() {
        var fakeQuery = A.Fake<IGetDataStoreQuery>();
        A.CallTo(() => fakeQuery.Execute(1)).Returns(new OdsInstance { OdsInstanceId = 1, Name = "DS1", InstanceType = "type", ConnectionString = "cs", OdsInstanceContexts = new List<OdsInstanceContext>(), OdsInstanceDerivatives = new List<OdsInstanceDerivative>() });
        var result = await ReadDataStore.GetDataStore(fakeQuery, 1);
        result.ShouldNotBeNull();
    }
}
