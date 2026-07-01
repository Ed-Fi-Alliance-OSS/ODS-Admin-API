// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.Vendors;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Profiles;

[TestFixture]
public class DeleteProfileTests
{
    [Test]
    public async Task Handle_DeletesProfileAndReturnsNoContent()
    {
        var command = A.Fake<IDeleteProfileCommand>();

        var result = await DeleteProfile.Handle(command, 12);

        result.ShouldBeOfType<NoContent>();
        A.CallTo(() => command.Execute(12)).MustHaveHappenedOnceExactly();
    }
}
