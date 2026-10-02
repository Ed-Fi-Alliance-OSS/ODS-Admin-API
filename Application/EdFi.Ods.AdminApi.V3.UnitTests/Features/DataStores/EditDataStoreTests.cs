// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores
{
    [TestFixture]
    public class EditDataStoreTests
    {
        [Test]
        public void Handle_WithMismatchedBodyId_ThrowsValidationException()
        {
            var request = new EditDataStore.EditDataStoreRequest
            {
                Id = 999,
                Name = "Test Data Store",
                DataStoreType = "Ods",
                ConnectionString = "Server=(local);Database=Test;Trusted_Connection=True;Encrypt=False"
            };

            var exception = Should.Throw<ValidationException>(() => EditDataStore.Handle(null!, null!, null!, null!, request, 1).GetAwaiter().GetResult());

            exception.Errors.Single(x => x.PropertyName == nameof(request.Id)).ErrorMessage
                .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
        }

        [Test]
        public async Task Validator_WithDuplicateName_ThrowsValidationExceptionWithDataStoreMessage()
        {
            var getDataStoresQuery = A.Fake<IGetDataStoresQuery>();
            A.CallTo(() => getDataStoresQuery.Execute()).Returns(
            [
                new OdsInstance { OdsInstanceId = 1, Name = "Original", InstanceType = "Production" },
                new OdsInstance { OdsInstanceId = 2, Name = "Existing", InstanceType = "Production" }
            ]);
            var getDataStoreQuery = A.Fake<IGetDataStoreQuery>();
            A.CallTo(() => getDataStoreQuery.Execute(1)).Returns(
                new OdsInstance { OdsInstanceId = 1, Name = "Original", InstanceType = "Production" });
            var validator = new EditDataStore.Validator(
                getDataStoresQuery,
                getDataStoreQuery,
                Options.Create(new AppSettings { DatabaseEngine = DatabaseEngineEnum.SqlServer }));
            var request = new EditDataStore.EditDataStoreRequest
            {
                Id = 1,
                Name = "Existing",
                DataStoreType = "Production"
            };

            var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

            exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.DataStoreAlreadyExistsMessage);
        }
    }
}
