// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.Vendors;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Profiles;

[TestFixture]
public class DeleteProfileTests
{
    [Test]
    public async Task Handle_DeletesProfileAndReturnsOk()
    {
        var command = A.Fake<IDeleteProfileCommand>();

        var result = await DeleteProfile.Handle(command, 12);

        result.ShouldBeOfType<Ok>();
        A.CallTo(() => command.Execute(12)).MustHaveHappenedOnceExactly();
    }
}
