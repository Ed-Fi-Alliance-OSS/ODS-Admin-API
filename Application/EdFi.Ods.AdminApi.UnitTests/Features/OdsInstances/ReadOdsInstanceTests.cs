// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class ReadOdsInstanceTests
{
    [Test]
    public async Task GetOdsInstances_ReturnsOkWithList()
    {
        var fakeGetAll = A.Fake<IGetOdsInstancesQuery>();
        A.CallTo(() => fakeGetAll.Execute(A<CommonQueryParams>._, null, null, null)).Returns(new List<OdsInstance>
        {
            new OdsInstance { OdsInstanceId = 1, Name = "ODS1", InstanceType = "type", ConnectionString = "cs" }
        });

        var result = await ReadOdsInstance.GetOdsInstances(fakeGetAll, new CommonQueryParams(0, 25), null, null, null);

        result.ShouldNotBeNull();
    }
}
