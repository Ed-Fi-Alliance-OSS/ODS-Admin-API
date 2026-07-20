// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.OdsInstanceDerivative;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;
using DbOdsInstance = EdFi.Admin.DataAccess.Models.OdsInstance;
namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstanceDerivative;
[TestFixture] public class ReadOdsInstanceDerivativeTests {
    [Test] public async Task GetOdsInstanceDerivatives_ReturnsOkWithList() {
        var fakeQuery = A.Fake<IGetOdsInstanceDerivativesQuery>();
        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._)).Returns(new List<DbOdsInstanceDerivative> {
            new DbOdsInstanceDerivative { OdsInstanceDerivativeId = 1, DerivativeType = "ReadReplica", ConnectionString = "cs" }
        });
        var result = await ReadOdsInstanceDerivative.GetOdsInstanceDerivatives(fakeQuery, new CommonQueryParams(0,25));
        result.ShouldNotBeNull();
    }
    [Test] public async Task GetOdsInstanceDerivative_WhenFound_ReturnsOk() {
        var fakeQuery = A.Fake<IGetOdsInstanceDerivativeByIdQuery>();
        var ods = new DbOdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        A.CallTo(() => fakeQuery.Execute(1)).Returns(new DbOdsInstanceDerivative { OdsInstanceDerivativeId = 1, OdsInstance = ods, DerivativeType = "ReadReplica", ConnectionString = "cs" });
        var result = await ReadOdsInstanceDerivative.GetOdsInstanceDerivative(fakeQuery, 1);
        result.ShouldNotBeNull();
    }
}
