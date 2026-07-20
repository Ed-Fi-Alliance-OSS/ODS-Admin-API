// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.ClaimSets;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApp.Management.ClaimSetEditor;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ClaimSets;

[TestFixture]
public class CopyClaimSetHandlerTests
{
    [Test]
    public async Task Handle_WithValidRequest_ReturnsCreated()
    {
        var fakeGetAll = A.Fake<IGetAllClaimSetsQuery>();
        A.CallTo(() => fakeGetAll.Execute()).Returns(new List<ClaimSet>());
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(1)).Returns(new ClaimSet { Id = 1, Name = "Source", IsEditable = true });
        var fakeCopyCommand = A.Fake<ICopyClaimSetCommand>();
        A.CallTo(() => fakeCopyCommand.Execute(A<ICopyClaimSetModel>._)).Returns(99);
        var fakeGetResources = A.Fake<IGetResourcesByClaimSetIdQuery>();
        var fakeGetApps = A.Fake<IGetApplicationsByClaimSetIdQuery>();
        A.CallTo(() => fakeGetApps.Execute(A<int>._)).Returns(new List<EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor.Application>());

        var validator = new CopyClaimSet.Validator(fakeGetAll, fakeGetById);
        var request = new CopyClaimSet.CopyClaimSetRequest { OriginalId = 1, Name = "CopiedCS" };

        var result = await CopyClaimSet.Handle(validator, fakeCopyCommand, fakeGetById, fakeGetResources, fakeGetApps, request);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created>();
    }

    [Test]
    public async Task Validator_WhenNameEmpty_FailsValidation()
    {
        var fakeGetAll = A.Fake<IGetAllClaimSetsQuery>();
        A.CallTo(() => fakeGetAll.Execute()).Returns(new List<ClaimSet>());
        var fakeGetById = A.Fake<IGetClaimSetByIdQuery>();
        A.CallTo(() => fakeGetById.Execute(A<int>._)).Returns(new ClaimSet { Id = 1, Name = "Source", IsEditable = true });

        var validator = new CopyClaimSet.Validator(fakeGetAll, fakeGetById);
        var result = await validator.ValidateAsync(new CopyClaimSet.CopyClaimSetRequest { OriginalId = 1, Name = "" });
        result.IsValid.ShouldBeFalse();
    }
}
