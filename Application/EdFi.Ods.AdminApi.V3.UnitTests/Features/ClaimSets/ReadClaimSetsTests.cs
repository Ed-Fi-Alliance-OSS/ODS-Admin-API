// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets;

[TestFixture]
public class ReadClaimSetsTests
{
    [Test]
    public async Task GetClaimSets_ReturnsOkWithList()
    {
        var fakeGetAll = A.Fake<IGetAllClaimSetsQuery>();
        var fakeGetApps = A.Fake<IGetApplicationsByClaimSetIdQuery>();
        A.CallTo(() => fakeGetAll.Execute(A<CommonQueryParams>._, null, null)).Returns(new List<ClaimSet>
        {
            new ClaimSet { Id = 1, Name = "CS1", IsEditable = true }
        });
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.Application>());

        var result = await ReadClaimSets.GetClaimSets(fakeGetAll, fakeGetApps, new CommonQueryParams(0, 25), null, null);

        result.ShouldNotBeNull();
    }
}
