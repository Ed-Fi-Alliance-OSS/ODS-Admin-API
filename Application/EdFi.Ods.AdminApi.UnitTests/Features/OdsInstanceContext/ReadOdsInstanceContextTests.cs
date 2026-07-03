// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.OdsInstanceContext;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
using DbOdsInstanceContext = EdFi.Admin.DataAccess.Models.OdsInstanceContext;
using DbOdsInstance = EdFi.Admin.DataAccess.Models.OdsInstance;
namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstanceContext;
[TestFixture] public class ReadOdsInstanceContextTests {
    [Test] public async Task GetOdsInstanceContexts_ReturnsOkWithList() {
        var fakeQuery = A.Fake<IGetOdsInstanceContextsQuery>();
        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._)).Returns(new List<DbOdsInstanceContext> {
            new DbOdsInstanceContext { OdsInstanceContextId = 1, ContextKey = "k", ContextValue = "v" }
        });
        var result = await ReadOdsInstanceContext.GetOdsInstanceContexts(fakeQuery, new CommonQueryParams(0,25));
        result.ShouldNotBeNull();
    }
    [Test] public async Task GetOdsInstanceContext_WhenFound_ReturnsOk() {
        var fakeQuery = A.Fake<IGetOdsInstanceContextByIdQuery>();
        var ods = new DbOdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        A.CallTo(() => fakeQuery.Execute(1)).Returns(new DbOdsInstanceContext { OdsInstanceContextId = 1, OdsInstance = ods, ContextKey = "k", ContextValue = "v" });
        var result = await ReadOdsInstanceContext.GetOdsInstanceContext(fakeQuery, 1);
        result.ShouldNotBeNull();
    }
}
