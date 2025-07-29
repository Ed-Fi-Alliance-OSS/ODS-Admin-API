// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.ApiClients;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class DeleteApiClientTests
    {
        [Test]
        public async Task Handle_ExecutesDeleteCommandAndReturnsOk()
        {
            // Arrange    
            var fakeCommand = A.Fake<IDeleteApiClientCommand>();
            int testId = 123;

            // Act    
            var result = await DeleteApiClient.Handle(fakeCommand, testId);

            // Assert    
            A.CallTo(() => fakeCommand.Execute(testId)).MustHaveHappenedOnceExactly();
            result.ShouldNotBeNull();
            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<object>>();
        }

        [Test]
        public void Handle_WhenCommandThrows_ExceptionIsPropagated()
        {
            // Arrange
            var fakeCommand = A.Fake<IDeleteApiClientCommand>();
            int testId = 999;
            A.CallTo(() => fakeCommand.Execute(testId)).Throws(new System.Exception("Delete failed"));

            // Act & Assert
            Should.Throw<System.Exception>(async () => await DeleteApiClient.Handle(fakeCommand, testId));
        }
    }
}
