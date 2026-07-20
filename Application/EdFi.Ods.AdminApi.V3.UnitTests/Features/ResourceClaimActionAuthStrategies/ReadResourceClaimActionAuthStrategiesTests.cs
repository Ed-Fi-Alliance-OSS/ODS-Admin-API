// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Features.ResourceClaimActionAuthStrategies;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ResourceClaimActionAuthStrategies;
[TestFixture] public class ReadResourceClaimActionAuthStrategiesTests {
    [Test] public async Task GetResourceClaimActionAuthorizationStrategies_ReturnsOkWithList() {
        var fakeQuery = A.Fake<IGetResourceClaimActionAuthorizationStrategiesQuery>();
        A.CallTo(() => fakeQuery.Execute(A<CommonQueryParams>._, null)).Returns(new List<ResourceClaimActionAuthStrategyModel> {
            new ResourceClaimActionAuthStrategyModel { ResourceClaimId = 1, ResourceName = "schools" }
        });
        var result = await ReadResourceClaimActionAuthStrategies.GetResourceClaimActionAuthorizationStrategies(fakeQuery, new CommonQueryParams(0,25), null);
        result.ShouldNotBeNull();
    }
}
