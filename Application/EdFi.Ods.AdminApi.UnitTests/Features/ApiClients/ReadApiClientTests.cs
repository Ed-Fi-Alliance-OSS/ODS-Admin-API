// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Features.ApiClients;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class ReadApiClientTests
    {
        [Test]
        public async Task GetApiClients_ReturnsOkWithMappedList()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientsByApplicationIdQuery>();
            var fakeMapper = A.Fake<IMapper>();
            int appId = 42;
            var queryResult = new List<ApiClient> { new ApiClient() };
            var mappedResult = new List<ApiClientModel> { new ApiClientModel { Id = 1 } };
            A.CallTo(() => fakeQuery.Execute(appId)).Returns(queryResult);
            A.CallTo(() => fakeMapper.Map<List<ApiClientModel>>(queryResult)).Returns(mappedResult);

            // Act
            var result = await ReadApiClient.GetApiClients(fakeQuery, fakeMapper, appId);

            // Assert
            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ApiClientModel>>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<ApiClientModel>>;
            okResult!.Value.ShouldBe(mappedResult);
        }

        [Test]
        public async Task GetApiClient_ReturnsOkWithMappedModel()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientByIdQuery>();
            var fakeMapper = A.Fake<IMapper>();
            int id = 7;
            var queryResult = new ApiClient();
            var mappedModel = new ApiClientModel { Id = id };
            A.CallTo(() => fakeQuery.Execute(id)).Returns(queryResult);
            A.CallTo(() => fakeMapper.Map<ApiClientModel>(queryResult)).Returns(mappedModel);

            // Act
            var result = await ReadApiClient.GetApiClient(fakeQuery, fakeMapper, id);

            // Assert
            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiClientModel>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiClientModel>;
            okResult!.Value.ShouldBe(mappedModel);
        }

        [Test]
        public void GetApiClient_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientByIdQuery>();
            var fakeMapper = A.Fake<IMapper>();
            int id = 99;
            A.CallTo(() => fakeQuery.Execute(id)).Returns(null);

            // Act & Assert
            Should.Throw<NotFoundException<int>>(() => ReadApiClient.GetApiClient(fakeQuery, fakeMapper, id).GetAwaiter().GetResult());
        }

        [Test]
        public void GetApiClients_WhenQueryThrows_ExceptionIsPropagated()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientsByApplicationIdQuery>();
            var fakeMapper = A.Fake<IMapper>();
            int appId = 42;
            A.CallTo(() => fakeQuery.Execute(appId)).Throws(new System.Exception("Query failed"));

            // Act & Assert
            Should.Throw<System.Exception>(async () => await ReadApiClient.GetApiClients(fakeQuery, fakeMapper, appId));
        }

        [Test]
        public void GetApiClient_WhenMapperThrows_ExceptionIsPropagated()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientByIdQuery>();
            var fakeMapper = A.Fake<IMapper>();
            int id = 7;
            var queryResult = new ApiClient();
            A.CallTo(() => fakeQuery.Execute(id)).Returns((ApiClient)queryResult);
            A.CallTo(() => fakeMapper.Map<ApiClientModel>(queryResult)).Throws(new System.Exception("Mapping failed"));

            // Act & Assert
            Should.Throw<System.Exception>(async () => await ReadApiClient.GetApiClient(fakeQuery, fakeMapper, id));
        }
    }
}
