// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets;
[TestFixture] public class ExportClaimSetTests {
    [Test] public async Task GetClaimSet_ReturnsOkWithDetails() {
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        A.CallTo(() => fakeGetResources.AllResources(1)).Returns(new List<ResourceClaim>());
        var fakeGetApps = A.Fake<IGetApplicationsByClaimSetIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<Application>());
        var result = await ExportClaimSet.GetClaimSet(fakeGetById, fakeGetResources, fakeGetApps, 1);
        result.ShouldNotBeNull();
    }

    [Test]
    public async Task GetClaimSet_ReturnsIdenticalPayloadShape_AsReadClaimSetsGetClaimSet()
    {
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        A.CallTo(() => fakeGetResources.AllResources(1)).Returns(new List<ResourceClaim>
        {
            new()
            {
                Name = "candidatePreparation",
                ClaimName = "http://ed-fi.org/identity/claims/candidatePreparation",
                Actions = new List<ResourceClaimAction> { new() { Name = "Read", Enabled = true } }
            }
        });
        var fakeGetApps = A.Fake<IGetApplicationsByClaimSetIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<Application>());

        var exportResult = await ExportClaimSet.GetClaimSet(fakeGetById, fakeGetResources, fakeGetApps, 1);
        var readResult = await ReadClaimSets.GetClaimSet(fakeGetById, fakeGetResources, fakeGetApps, 1);

        var exportValue = ((Microsoft.AspNetCore.Http.HttpResults.Ok<ClaimSetDetailsModel>)exportResult).Value;
        var readValue = ((Microsoft.AspNetCore.Http.HttpResults.Ok<ClaimSetDetailsModel>)readResult).Value;

        var exportJson = System.Text.Json.JsonSerializer.Serialize(exportValue);
        var readJson = System.Text.Json.JsonSerializer.Serialize(readValue);

        exportJson.ShouldBe(readJson);
    }
}
