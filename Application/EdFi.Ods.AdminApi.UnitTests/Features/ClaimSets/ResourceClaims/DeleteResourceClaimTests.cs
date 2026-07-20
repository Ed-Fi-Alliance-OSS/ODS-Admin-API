// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.ClaimSets.ResourceClaims;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.UnitTests.Features.ClaimSets.ResourceClaims;
[TestFixture] public class DeleteResourceClaimTests {
    [Test] public async Task Handle_WhenEditableAndResourceExists_ReturnsOk() {
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        A.CallTo(() => fakeGetResources.SingleResource(1, 10)).Returns(new ResourceClaim { Id = 10, Name = "schools" });
        var fakeDeleteCommand = A.Fake<IDeleteResouceClaimOnClaimSetCommand>();
        var fakeStrategyResolver = A.Fake<IAuthStrategyResolver>();
        var result = await DeleteResourceClaim.Handle(fakeGetResources, fakeGetById, fakeStrategyResolver, fakeDeleteCommand, 1, 10);
        result.ShouldNotBeNull();
        A.CallTo(() => fakeDeleteCommand.Execute(1, 10)).MustHaveHappenedOnceExactly();
    }
    [Test] public void Handle_WhenSystemReserved_ThrowsValidationException() {
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "SysCS", IsEditable = false });
        var fakeDeleteCommand = A.Fake<IDeleteResouceClaimOnClaimSetCommand>();
        var fakeStrategyResolver = A.Fake<IAuthStrategyResolver>();
        Should.Throw<FluentValidation.ValidationException>(() =>
            DeleteResourceClaim.Handle(fakeGetResources, fakeGetById, fakeStrategyResolver, fakeDeleteCommand, 1, 10).GetAwaiter().GetResult());
    }
}
