// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class DeleteApplicationTests
{
    [Test]
    public async Task Handle_ExecutesDeleteCommandAndReturnsOk()
    {
        var command = A.Fake<IDeleteApplicationCommand>();
        var id = 123;

        var result = await DeleteApplication.Handle(command, id);

        A.CallTo(() => command.Execute(id)).MustHaveHappenedOnceExactly();
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<object>>();
    }

    [Test]
    public void Handle_WhenCommandThrows_ExceptionIsPropagated()
    {
        var command = A.Fake<IDeleteApplicationCommand>();
        A.CallTo(() => command.Execute(999)).Throws(new System.Exception("Delete failed"));

        Should.Throw<System.Exception>(async () => await DeleteApplication.Handle(command, 999));
    }
}
