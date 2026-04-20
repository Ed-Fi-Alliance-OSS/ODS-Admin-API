// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Features.ApiClients;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V3.Features.ApiClients
{
    [TestFixture]
    public class ReadApiClientTests
    {
        [Test]
        public async Task GetApiClients_ReturnsOkWithMappedList()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientsByApplicationIdQuery>();
            var fakeOdsQuery = A.Fake<IGetOdsInstanceIdsByApiClientIdQuery>();
            int appId = 42;
            var apiClient = new ApiClient
            {
                ApiClientId = 1,
                Name = "Test",
                Application = new Application { ApplicationId = appId },
                ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
            };
            A.CallTo(() => fakeQuery.Execute(appId)).Returns(new List<ApiClient> { apiClient });
            A.CallTo(() => fakeOdsQuery.Execute(A<IEnumerable<int>>._)).Returns(new Dictionary<int, IList<int>>());

            // Act
            var result = await ReadApiClient.GetApiClients(fakeQuery, fakeOdsQuery, appId);

            // Assert
            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ApiClientModel>>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<List<ApiClientModel>>;
            okResult!.Value!.Count.ShouldBe(1);
            okResult.Value[0].Id.ShouldBe(1);
        }

        [Test]
        public async Task GetApiClient_ReturnsOkWithMappedModel()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientByIdQuery>();
            var fakeOdsQuery = A.Fake<IGetOdsInstanceIdsByApiClientIdQuery>();
            int id = 7;
            var apiClient = new ApiClient
            {
                ApiClientId = id,
                Name = "Client",
                Application = new Application { ApplicationId = 1 },
                ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
            };
            A.CallTo(() => fakeQuery.Execute(id)).Returns(apiClient);
            A.CallTo(() => fakeOdsQuery.Execute(id)).Returns(new List<int> { 10, 20 });

            // Act
            var result = await ReadApiClient.GetApiClient(fakeQuery, fakeOdsQuery, id);

            // Assert
            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiClientModel>>();
            var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiClientModel>;
            okResult!.Value!.Id.ShouldBe(id);
            okResult.Value.OdsInstanceIds.ShouldBe(new List<int> { 10, 20 });
        }

        [Test]
        public void GetApiClient_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientByIdQuery>();
            var fakeOdsQuery = A.Fake<IGetOdsInstanceIdsByApiClientIdQuery>();
            int id = 99;
            A.CallTo(() => fakeQuery.Execute(id)).Returns(null);

            // Act & Assert
            Should.Throw<NotFoundException<int>>(() => ReadApiClient.GetApiClient(fakeQuery, fakeOdsQuery, id).GetAwaiter().GetResult());
        }

        [Test]
        public void GetApiClients_WhenQueryThrows_ExceptionIsPropagated()
        {
            // Arrange
            var fakeQuery = A.Fake<IGetApiClientsByApplicationIdQuery>();
            var fakeOdsQuery = A.Fake<IGetOdsInstanceIdsByApiClientIdQuery>();
            int appId = 42;
            A.CallTo(() => fakeQuery.Execute(appId)).Throws(new System.Exception("Query failed"));

            // Act & Assert
            Should.Throw<System.Exception>(async () => await ReadApiClient.GetApiClients(fakeQuery, fakeOdsQuery, appId));
        }
    }
}



