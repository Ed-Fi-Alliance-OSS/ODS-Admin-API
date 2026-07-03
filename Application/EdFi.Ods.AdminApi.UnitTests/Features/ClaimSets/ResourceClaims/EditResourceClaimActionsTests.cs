// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.ClaimSets.ResourceClaims;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;
using SecurityAction = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ClaimSets.ResourceClaims;

[TestFixture]
public class EditResourceClaimActionsTests
{
    [Test]
    public async Task Validator_WhenNullResourceClaimActions_FailsValidation()
    {
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(A<int>._)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        var fakeGetFlat = A.Fake<IGetResourceClaimsAsFlatListQuery>();
        A.CallTo(() => fakeGetFlat.Execute()).Returns(new List<ResourceClaim>());
        var fakeGetActions = A.Fake<IGetAllActionsQuery>();
        A.CallTo(() => fakeGetActions.Execute()).Returns(new List<SecurityAction>());

        var validator = new EditResourceClaimActions.ResourceClaimClaimSetValidator(fakeGetById, fakeGetFlat, fakeGetActions);
        var request = new EditResourceClaimActions.AddResourceClaimOnClaimSetRequest
        {
            ClaimSetId = 1, ResourceClaimId = 1, ResourceClaimActions = null
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task Validator_WhenSystemReservedClaimSet_FailsValidation()
    {
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(A<int>._)).Returns(new ClaimSet { Id = 1, Name = "SysCS", IsEditable = false });
        var fakeGetFlat = A.Fake<IGetResourceClaimsAsFlatListQuery>();
        A.CallTo(() => fakeGetFlat.Execute()).Returns(new List<ResourceClaim>
        {
            new ResourceClaim { Id = 10, Name = "schools" }
        });
        var fakeGetActions = A.Fake<IGetAllActionsQuery>();
        A.CallTo(() => fakeGetActions.Execute()).Returns(new List<SecurityAction>
        {
            new SecurityAction { ActionId = 1, ActionName = "Read", ActionUri = "uri" }
        });

        var validator = new EditResourceClaimActions.ResourceClaimClaimSetValidator(fakeGetById, fakeGetFlat, fakeGetActions);
        var request = new EditResourceClaimActions.AddResourceClaimOnClaimSetRequest
        {
            ClaimSetId = 1,
            ResourceClaimId = 10,
            ResourceClaimActions = new List<ResourceClaimAction> { new ResourceClaimAction { Name = "Read", Enabled = true } }
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.ShouldBeFalse();
    }
}
