// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreContexts;
[TestFixture] public class ReadDataStoreContextTests {
    [Test] public async Task GetDataStoreContexts_ReturnsOkWithList() {
        var fakeQuery = A.Fake<IGetDataStoreContextsQuery>();
        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._)).Returns(new List<OdsInstanceContext> {
            new OdsInstanceContext { OdsInstanceContextId = 1, ContextKey = "k", ContextValue = "v" }
        });
        var result = await ReadDataStoreContext.GetDataStoreContexts(fakeQuery, new CommonQueryParams(0,25));
        result.ShouldNotBeNull();
    }
    [Test] public async Task GetDataStoreContext_WhenFound_ReturnsOk() {
        var fakeQuery = A.Fake<IGetDataStoreContextByIdQuery>();
        var ods = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        A.CallTo(() => fakeQuery.Execute(1)).Returns(new OdsInstanceContext { OdsInstanceContextId = 1, OdsInstance = ods, ContextKey = "k", ContextValue = "v" });
        var result = await ReadDataStoreContext.GetDataStoreContext(fakeQuery, 1);
        result.ShouldNotBeNull();
    }
}
