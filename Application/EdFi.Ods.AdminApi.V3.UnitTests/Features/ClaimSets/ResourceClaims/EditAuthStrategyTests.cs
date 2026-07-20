// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets.ResourceClaims;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;
using SecurityAction = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets.ResourceClaims;

[TestFixture]
public class EditAuthStrategyTests
{
    [Test]
    public async Task OverrideValidator_WhenClaimSetIdIsZero_FailsValidation()
    {
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        var fakeGetAllAuthStrategies = A.Fake<IGetAllAuthorizationStrategiesQuery>();
        A.CallTo(() => fakeGetAllAuthStrategies.Execute()).Returns(new List<AuthorizationStrategy>());
        var fakeGetAllActions = A.Fake<IGetAllActionsQuery>();
        A.CallTo(() => fakeGetAllActions.Execute()).Returns(new List<SecurityAction>());
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(A<int>._)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });

        var validator = new EditAuthStrategy.OverrideAuthStategyOnClaimSetValidator(fakeGetResources, fakeGetAllAuthStrategies, fakeGetAllActions, fakeGetById);
        var request = new EditAuthStrategy.OverrideAuthStategyOnClaimSetRequest
        {
            ClaimSetId = 0, ResourceClaimId = 1, ActionName = "Read",
            AuthorizationStrategies = new List<string> { "NoFurtherAuth" }
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task OverrideValidator_WhenAuthorizationStrategiesEmpty_FailsValidation()
    {
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        var fakeGetAllAuthStrategies = A.Fake<IGetAllAuthorizationStrategiesQuery>();
        A.CallTo(() => fakeGetAllAuthStrategies.Execute()).Returns(new List<AuthorizationStrategy>());
        var fakeGetAllActions = A.Fake<IGetAllActionsQuery>();
        A.CallTo(() => fakeGetAllActions.Execute()).Returns(new List<SecurityAction>());
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(A<int>._)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });

        var validator = new EditAuthStrategy.OverrideAuthStategyOnClaimSetValidator(fakeGetResources, fakeGetAllAuthStrategies, fakeGetAllActions, fakeGetById);
        var request = new EditAuthStrategy.OverrideAuthStategyOnClaimSetRequest
        {
            ClaimSetId = 1, ResourceClaimId = 1, ActionName = "Read",
            AuthorizationStrategies = new List<string>()
        };
        var result = await validator.ValidateAsync(request);
        result.IsValid.ShouldBeFalse();
    }
}
