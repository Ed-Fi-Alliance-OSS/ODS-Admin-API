// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreDerivatives;
[TestFixture] public class ReadDataStoreDerivativeTests {
    [Test] public async Task GetDataStoreDerivatives_ReturnsOkWithList() {
        var fakeQuery = A.Fake<IGetDataStoreDerivativesQuery>();
        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._)).Returns(new List<OdsInstanceDerivative> {
            new OdsInstanceDerivative { OdsInstanceDerivativeId = 1, DerivativeType = "ReadReplica", ConnectionString = "cs" }
        });
        var result = await ReadDataStoreDerivative.GetDataStoreDerivatives(fakeQuery, new CommonQueryParams(0,25));
        result.ShouldNotBeNull();
    }
    [Test] public async Task GetDataStoreDerivative_WhenFound_ReturnsOk() {
        var fakeQuery = A.Fake<IGetDataStoreDerivativeByIdQuery>();
        var ods = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        A.CallTo(() => fakeQuery.Execute(1)).Returns(new OdsInstanceDerivative { OdsInstanceDerivativeId = 1, OdsInstance = ods, DerivativeType = "ReadReplica", ConnectionString = "cs" });
        var result = await ReadDataStoreDerivative.GetDataStoreDerivative(fakeQuery, 1);
        result.ShouldNotBeNull();
    }
}
