// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets;

[TestFixture]
public class EditClaimSetHandlerTests
{
    [Test]
    public async Task Handle_WithValidRequest_ReturnsNoContent()
    {
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        var fakeGetAll = A.Fake<IGetAllClaimSetsQuery>();
        A.CallTo(() => fakeGetAll.Execute()).Returns(new List<ClaimSet>());
        var fakeEditCommand = A.Fake<IEditClaimSetCommand>();
        A.CallTo(() => fakeEditCommand.Execute(A<IEditClaimSetModel>._)).Returns(1);

        var validator = new EditClaimSet.Validator(fakeGetById, fakeGetAll);
        var request = new EditClaimSet.EditClaimSetRequest { Id = 1, Name = "NewName" };

        var editClaimSet = new EditClaimSet();
        var result = await editClaimSet.Handle(validator, fakeEditCommand, request, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Test]
    public async Task Validator_WhenNameEmpty_FailsValidation()
    {
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(A<int>._)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        var fakeGetAll = A.Fake<IGetAllClaimSetsQuery>();
        A.CallTo(() => fakeGetAll.Execute()).Returns(new List<ClaimSet>());

        var validator = new EditClaimSet.Validator(fakeGetById, fakeGetAll);
        var result = await validator.ValidateAsync(new EditClaimSet.EditClaimSetRequest { Id = 1, Name = "" });
        result.IsValid.ShouldBeFalse();
    }
}
