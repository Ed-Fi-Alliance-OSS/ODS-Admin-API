// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.ResourceClaims;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ResourceClaims;

[TestFixture]
public class ReadResourceClaimsTests
{
    [Test]
    public async Task GetResourceClaims_ReturnsOkWithList()
    {
        var fakeGetAll = A.Fake<IGetResourceClaimsQuery>();
        A.CallTo(() => fakeGetAll.Execute(A<CommonQueryParams>._, null, null)).Returns(new List<ResourceClaim>
        {
            new ResourceClaim { Id = 1, Name = "schools" }
        });

        var result = await ReadResourceClaims.GetResourceClaims(fakeGetAll, new CommonQueryParams(0, 25), null, null);

        result.ShouldNotBeNull();
    }
}
