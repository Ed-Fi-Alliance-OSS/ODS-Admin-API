// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets;

[TestFixture]
public class DeleteClaimSetHandlerTests
{
    [Test]
    public async Task Handle_WithEditableClaimSetAndNoApps_ReturnsNoContent()
    {
        var fakeDeleteCommand = A.Fake<IDeleteClaimSetCommand>();
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "CS1", IsEditable = true });
        var fakeGetApps = A.Fake<IGetApplicationsByClaimSetIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.Application>());

        var deleteClaimSet = new DeleteClaimSet();
        var result = await deleteClaimSet.Handle(fakeDeleteCommand, fakeGetById, fakeGetApps, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        A.CallTo(() => fakeDeleteCommand.Execute(A<IDeleteClaimSetModel>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Handle_WithSystemReservedClaimSet_ThrowsValidationException()
    {
        var fakeDeleteCommand = A.Fake<IDeleteClaimSetCommand>();
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "SystemCS", IsEditable = false });
        var fakeGetApps = A.Fake<IGetApplicationsByClaimSetIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.Application>());

        var deleteClaimSet = new DeleteClaimSet();
        Should.Throw<FluentValidation.ValidationException>(() =>
            deleteClaimSet.Handle(fakeDeleteCommand, fakeGetById, fakeGetApps, 1).GetAwaiter().GetResult());
    }
}
