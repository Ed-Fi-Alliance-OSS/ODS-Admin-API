// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using AutoMapper;
using EdFi.Ods.AdminApi.Features.ApiClients;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class ResetApiClientCredentialsTests
    {
        [Test]
        public async Task HandleResetCredentials_ExecutesCommandAndReturnsOk()
        {
            // Arrange  
            var fakeCommand = A.Fake<IRegenerateApiClientSecretCommand>();
            var fakeMapper = A.Fake<IMapper>();
            int id = 123;
            var commandResult = new RegenerateApiClientSecretResult();
            var mappedResult = new ApiClientResult { Id = id, Key = "key", Secret = "secret" };
            A.CallTo(() => fakeCommand.Execute(id)).Returns(commandResult);
            A.CallTo(() => fakeMapper.Map<ApiClientResult>(commandResult)).Returns(mappedResult);

            // Act  
            var result = await ResetApiClientCredentials.HandleResetCredentials(fakeCommand, fakeMapper, id);

            // Assert  
            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiClientResult>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiClientResult>;
            okResult!.Value.ShouldBe(mappedResult);
        }

        [Test]
        public void HandleResetCredentials_WhenCommandThrows_ExceptionIsPropagated()
        {
            // Arrange
            var fakeCommand = A.Fake<IRegenerateApiClientSecretCommand>();
            var fakeMapper = A.Fake<IMapper>();
            int id = 999;
            A.CallTo(() => fakeCommand.Execute(id)).Throws(new System.Exception("Reset failed"));

            // Act & Assert
            Should.Throw<System.Exception>(async () => await ResetApiClientCredentials.HandleResetCredentials(fakeCommand, fakeMapper, id));
        }
    }
}
